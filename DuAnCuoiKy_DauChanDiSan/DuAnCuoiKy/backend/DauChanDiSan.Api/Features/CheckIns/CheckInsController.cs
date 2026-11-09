using System.ComponentModel.DataAnnotations;
using DauChanDiSan.Api.Features.CheckIns.DTOs;
using DauChanDiSan.Api.Features.CheckIns.Repositories;
using DauChanDiSan.Api.Shared;
using DauChanDiSan.Api.Shared.DTOs;
using DauChanDiSan.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DauChanDiSan.Api.Features.CheckIns;

[ApiController, Route("api/check-in"), Authorize(Policy = Roles.Account)]
public sealed class CheckInsController(CheckInRepository checkIns, CurrentUser user) : ControllerBase
{
    [HttpPost]
    public Task<CheckInReceipt> Create(CheckInInput input, CancellationToken ct)
    {
        MediaValidation.RequireKind(input.AnhPath, input.VideoPath);
        MediaValidation.RequireOwner(user.Id, input.AnhPath, input.VideoPath);
        return checkIns.RecordAsync(user.Id, input, ct);
    }

    [HttpGet]
    public Task<PageResult<CheckInDto>> Mine([Range(1, 5000)] int trang = 1, CancellationToken ct = default) =>
        checkIns.ListAsync(user.Id, trang, ct);

    [HttpGet("{id:long}")]
    public Task<CheckInDto> Detail(long id, CancellationToken ct) => checkIns.DetailAsync(user.Id, id, ct);

    [HttpPut("{id:long}/hanh-trinh/{hanhTrinhId:long}")]
    public async Task<IActionResult> Attach(long id, long hanhTrinhId, CancellationToken ct)
    {
        await checkIns.AttachAsync(user.Id, id, hanhTrinhId, ct);
        return NoContent();
    }
}
