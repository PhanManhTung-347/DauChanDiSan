using System.Security.Claims;

namespace DauChanDiSan.Api.Shared;

public static class Roles
{
    public const string Admin = "QUAN_TRI";
    public const string Partner = "DOI_TAC";
    public const string Culture = "KIEM_DUYET_VAN_HOA";
    public const string HeritageModerators = Admin + "," + Culture;
    public const string Member = "THANH_VIEN";
    public const string Account = "TaiKhoanDaDongBo";
}

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public string Uid => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw ApiFault.Denied();
    public int? OptionalId => int.TryParse(Principal?.FindFirstValue("sql_id"), out var id) ? id : null;
    public int Id => OptionalId ?? throw ApiFault.Denied();
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);
    public string? DisplayName => Principal?.FindFirstValue(ClaimTypes.Name);
    public bool EmailVerified => Principal?.HasClaim("email_verified", "true") == true;
    public bool Admin => Principal?.IsInRole(Roles.Admin) == true;
    public bool CanReviewHeritage => Admin || Principal?.IsInRole(Roles.Culture) == true;
    public bool Partner => Principal?.IsInRole(Roles.Partner) == true;
}
