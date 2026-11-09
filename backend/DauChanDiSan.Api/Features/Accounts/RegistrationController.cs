using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using DauChanDiSan.Api.Infrastructure.Firebase;
using DauChanDiSan.Api.Shared;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DauChanDiSan.Api.Features.Accounts;

public sealed record SendRegistrationCode([Required, EmailAddress, StringLength(254)] string Email);
public sealed record ConfirmRegistration(
    [Required, EmailAddress, StringLength(254)] string Email, Guid MaYeuCau,
    [Required, RegularExpression("[0-9]{6}")] string Ma,
    [Required, StringLength(120)] string HoTen,
    [Required, StringLength(128, MinimumLength = 8)] string MatKhau);

[ApiController, Route("api/dang-ky"), EnableRateLimiting("register")]
public sealed class RegistrationController(IConfiguration config, SqlProcedures sql, FirebaseIdentity firebase,
    ILogger<RegistrationController> logger) : ControllerBase
{
    [HttpPost("gui-ma")]
    public async Task<IActionResult> Send(SendRegistrationCode input, CancellationToken ct)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var host = config["Registration:Smtp:Host"];
        var from = config["Registration:Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new ApiFault(503, "Hệ thống chưa cấu hình gửi email.");
        var id = Guid.NewGuid();
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var result = (await sql.RunAsync("dbo.usp_TaoMaDangKy", ct,
            ("Email", email), ("MaYeuCau", id), ("MaBam", Digest(id, code)))).Single();
        if (result["DuocGui"] is not true)
            throw new ApiFault(429, "Vui lòng chờ 60 giây trước khi gửi lại.");
        var emailBody = $"""
        <!doctype html>
        <html lang="vi">
        <body style="margin:0;padding:24px;background:#f7f2ea;color:#342622;font-family:Arial,sans-serif;">
          <div style="max-width:560px;margin:0 auto;background:#fffdfa;border:1px solid #e5d8c2;border-radius:12px;">
            <div style="padding:22px 28px;background:#8a2938;color:#fffdfa;border-radius:11px 11px 0 0;">
              <div style="font-size:22px;font-weight:bold;">Dấu Chân Di Sản</div>
              <div style="margin-top:6px;color:#f4d8a2;font-size:13px;">Hành trình qua ký ức</div>
            </div>

            <div style="padding:24px 28px;font-size:15px;line-height:1.65;">
              <h1 style="margin:0 0 16px;font-size:23px;color:#651b29;">
                Xác nhận địa chỉ email của bạn
              </h1>

              <p>Chào bạn,</p>
              <p>Cảm ơn bạn đã đăng ký Dấu Chân Di Sản. Tài khoản của bạn sẽ là nơi
              lưu lại những di sản đã ghé thăm, các câu chuyện yêu thích và những
              khoảnh khắc trên hành trình khám phá Việt Nam.</p>

              <p>Để hoàn tất đăng ký, hãy quay lại trang web và nhập mã xác nhận sau.</p>

              <div style="margin:20px 0;padding:16px;text-align:center;background:#faf0e7;border:1px solid #e6cda3;border-radius:8px;">
                <span style="font-family:monospace;font-size:30px;font-weight:bold;letter-spacing:6px;color:#8a2938;">{code}</span>
              </div>

              <p>Mã có hiệu lực trong <strong>10 phút</strong>. Bạn chỉ nên nhập mã
              trên trang đăng ký Dấu Chân Di Sản và giữ mã cho riêng mình.</p>

              <p>Nếu bạn không yêu cầu đăng ký, bạn có thể bỏ qua email này.</p>

              <p>Hẹn gặp bạn trên hành trình khám phá,<br>
              <strong>Đội ngũ Dấu Chân Di Sản</strong></p>
            </div>
          </div>
        </body>
        </html>
        """;

        using var message = new MailMessage(
            from, email, "Xác nhận đăng ký · Dấu Chân Di Sản", emailBody)
        {
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        using var smtp = new SmtpClient(host, config.GetValue("Registration:Smtp:Port", 587)) { EnableSsl = true };
        if (config["Registration:Smtp:User"] is { Length: > 0 } user)
            smtp.Credentials = new NetworkCredential(user, config["Registration:Smtp:Password"]);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(25));
            await smtp.SendMailAsync(message, timeout.Token);
        }
        catch (Exception ex) when (ex is SmtpException or OperationCanceledException)
        {
            logger.LogWarning("Không gửi được OTP qua SMTP ({ErrorType}).", ex.GetType().Name);
            await sql.QueryAsync("UPDATE dbo.MaDangKyEmail SET DaDung=1 WHERE Email=@Email AND MaYeuCau=@Id;",
                CancellationToken.None, ("Email", email), ("Id", id));
            throw new ApiFault(503, "Chưa gửi được mã. Vui lòng thử lại sau.");
        }
        return Ok(new { maYeuCau = id });
    }

    [HttpPost("xac-nhan")]
    public async Task<IActionResult> Confirm(ConfirmRegistration input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.HoTen)) throw ApiFault.Invalid("Nhập họ và tên.");
        var email = input.Email.Trim().ToLowerInvariant();
        var result = (await sql.RunAsync("dbo.usp_XacNhanMaDangKy", ct, ("Email", email),
            ("MaYeuCau", input.MaYeuCau), ("MaBam", Digest(input.MaYeuCau, input.Ma)))).Single();
        if (result["HopLe"] is not true) throw ApiFault.Invalid("Mã không đúng, đã hết hạn hoặc đã vượt 5 lần thử.");
        try { await firebase.CreateVerifiedAsync(email, input.HoTen.Trim(), input.MatKhau, ct); }
        catch (FirebaseAuthException e) when (e.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
        { throw new ApiFault(409, "Email đã có tài khoản. Hãy đăng nhập hoặc đặt lại mật khẩu."); }
        catch (FirebaseAuthException e)
        {
            logger.LogWarning("Firebase từ chối tạo tài khoản ({Code}).", e.AuthErrorCode);
            throw new ApiFault(503, "Chưa tạo được tài khoản. Bạn có thể yêu cầu mã mới.");
        }
        return NoContent();
    }

    private byte[] Digest(Guid id, string code)
    {
        var secret = config["Registration:OtpSecret"];
        if (secret is not { Length: >= 32 }) throw new ApiFault(503, "Hệ thống chưa cấu hình xác minh email.");
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{id:N}:{code}"));
    }
}
