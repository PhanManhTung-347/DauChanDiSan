using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Features.Discovery.DTOs;
using DauChanDiSan.Api.Features.Discovery.Repositories;
using DauChanDiSan.Api.Infrastructure.Storage;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Shared.DTOs;
using DauChanDiSan.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DauChanDiSan.Api.Features.Discovery;

[ApiController, Route("api/dong-gop-di-san"), Authorize(Policy = Roles.Account)]
public sealed class ContributionsController(ContributionRepository contributions, MediaStorage storage, CurrentUser user) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ContributionDto>> Submit(ContributionInput input, CancellationToken ct)
    {
        MediaValidation.RequireKind(input.AnhPath, null);
        MediaValidation.RequireOwner(user.Id, input.AnhPath);
        if (input.AnhPath is not null) await storage.EnsureImageExistsAsync(input.AnhPath, ct);
        var (row, created) = await contributions.SubmitAsync(user.Id, input, ct);
        return created ? CreatedAtAction(nameof(Detail), new { id = row.DongGopId }, row) : Ok(row);
    }

    [HttpGet("toi")]
    public Task<PageResult<ContributionDto>> Mine([Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        contributions.ListAsync(user.Id, null, trang, ct);

    [HttpGet("{id:long}")]
    public Task<ContributionDto> Detail(long id, CancellationToken ct) => contributions.FindAsync(id, user.Id, ct);

    [HttpPut("{id:long}/rut")]
    public async Task<IActionResult> Withdraw(long id, CancellationToken ct)
    {
        await contributions.WithdrawAsync(user.Id, id, ct);
        return NoContent();
    }
}

[ApiController, Route("api/quan-tri/dong-gop-di-san"), Authorize(Roles = Roles.HeritageModerators)]
public sealed class ContributionAdminController(ContributionRepository contributions, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<ContributionDto>> List(
        [OneOf("CHO_DUYET", "DA_DUYET", "TU_CHOI", "DA_RUT")] string? trangThai = "CHO_DUYET",
        [Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        contributions.ListAsync(null, trangThai, trang, ct);

    [HttpGet("{id:long}")]
    public Task<ContributionDto> Detail(long id, CancellationToken ct) => contributions.FindAsync(id, null, ct);

    [HttpPut("{id:long}/duyet")]
    public Task<ContributionDto> Review(long id, ReviewContributionInput input, CancellationToken ct) =>
        contributions.ReviewAsync(user.Id, id, input, ct);
}
