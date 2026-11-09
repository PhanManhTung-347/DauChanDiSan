using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Data.Entities;
using DauChanDiSan.Api.Shared;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DauChanDiSan.Api.Infrastructure.Firebase;

public sealed class FirebaseIdentity(IConfiguration config)
{
    private readonly SemaphoreSlim gate = new(1);
    private FirebaseAuth? auth;

    private async Task<FirebaseAuth> GetAuthAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (auth is null)
            {
                var projectId = config["Firebase:ProjectId"];
                if (string.IsNullOrWhiteSpace(projectId))
                    throw new ApiFault(503, "Chưa cấu hình Firebase:ProjectId.");
                GoogleCredential credential;
                try { credential = await GoogleCredential.GetApplicationDefaultAsync(ct); }
                catch (InvalidOperationException) { throw new ApiFault(503, "Chưa cấu hình Google Application Default Credentials."); }
                var app = FirebaseApp.Create(new AppOptions { ProjectId = projectId, Credential = credential }, "DauChanDiSan");
                auth = FirebaseAuth.GetAuth(app);
            }
        }
        finally { gate.Release(); }

        return auth;
    }
    public async Task<FirebaseToken> VerifyAsync(string idToken, CancellationToken ct) =>
        await (await GetAuthAsync(ct)).VerifyIdTokenAsync(idToken, checkRevoked: true, cancellationToken: ct);

    public async Task CreateVerifiedAsync(string email, string name, string password, CancellationToken ct) =>
        await (await GetAuthAsync(ct)).CreateUserAsync(new UserRecordArgs
        { Email = email, DisplayName = name, Password = password, EmailVerified = true }, ct);
}

public sealed class FirebaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    FirebaseIdentity firebase, DauChanDiSanDbContext db, IWebHostEnvironment environment, IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        NguoiDung? user = null;
        string uid;
        string? email;
        string? name;
        bool verified;
        var ct = Context.RequestAborted;
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.Length > 0)
        {
            if (!AuthenticationHeaderValue.TryParse(authorization, out var header)
                || !header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(header.Parameter))
                return AuthenticateResult.Fail("Cần Firebase ID token dạng Bearer.");
            try
            {
                var token = await firebase.VerifyAsync(header.Parameter, ct);
                uid = token.Uid;
                email = token.Claims.GetValueOrDefault("email")?.ToString();
                name = token.Claims.GetValueOrDefault("name")?.ToString();
                verified = token.Claims.GetValueOrDefault("email_verified") is true;
                if (!verified || string.IsNullOrWhiteSpace(email))
                    return AuthenticateResult.Fail("Email chưa được xác minh.");
            }
            catch (FirebaseAuthException)
            {
                return AuthenticateResult.Fail("Firebase ID token không hợp lệ hoặc đã hết hạn.");
            }
        }
        else if (environment.IsDevelopment() && Request.Headers.TryGetValue("X-Dev-Uid", out var value))
        {
            var expected = config["DevAuth:Key"];
            var provided = Request.Headers["X-Dev-Key"].ToString();
            uid = value.ToString();
            if (expected is null || expected.Length < 24 || uid is not ("demo-admin" or "demo-lan" or "demo-khoi" or "demo-doi-tac" or "demo-van-hoa")
                || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided)))
                return AuthenticateResult.Fail("Thông tin tài khoản demo không hợp lệ.");
            user = await db.NguoiDungs.AsNoTracking().SingleOrDefaultAsync(x => x.FirebaseUid == uid, ct);
            if (user is null)
                return AuthenticateResult.Fail("Chưa có tài khoản demo trong SQL Server.");
            (email, name, verified) = (user.Email, user.HoTen, user.EmailDaXacMinh);
        }
        else
        {
            return AuthenticateResult.NoResult();
        }

        user ??= await db.NguoiDungs.AsNoTracking().SingleOrDefaultAsync(x => x.FirebaseUid == uid, ct);
        if (user?.HoatDong == false)
            return AuthenticateResult.Fail("Tài khoản đã bị khóa.");

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, uid) };
        if (email is not null) claims.Add(new Claim(ClaimTypes.Email, email));
        if (name is not null) claims.Add(new Claim(ClaimTypes.Name, name));
        if (verified) claims.Add(new Claim("email_verified", "true"));
        if (user is not null)
        {
            claims.Add(new Claim("sql_id", user.NguoiDungId.ToString()));
            var roles = await db.NguoiDungVaiTros.Where(x => x.NguoiDungId == user.NguoiDungId)
                .Select(x => x.MaVaiTro).ToListAsync(ct);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }
        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
