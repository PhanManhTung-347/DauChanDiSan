using DauChanDiSan.Api.Features.Trips.DTOs;
using DauChanDiSan.Api.Features.Trips.Repositories;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DauChanDiSan.Api.Features.Trips;

[ApiController, Route("api/ai/hoi-thoai"), Authorize(Policy = Roles.Account)]
public sealed class AssistantController(ConversationRepository conversations, TripAssistantService assistant, CurrentUser user) : ControllerBase
{
    [HttpGet]
    public Task<List<ConversationDto>> List(CancellationToken ct) => conversations.ListAsync(user.Id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(ConversationInput input, CancellationToken ct)
    {
        var title = input.TieuDe.Trim();
        var id = await conversations.CreateAsync(user.Id, title, ct);
        return CreatedAtAction(nameof(Detail), new { id }, new { hoiThoaiId = id, tieuDe = title });
    }

    [HttpGet("{id:long}")]
    public Task<ConversationDetail> Detail(long id, CancellationToken ct) => conversations.DetailAsync(user.Id, id, ct);

    [HttpPut("{id:long}/luu-tru")]
    public async Task<IActionResult> Archive(long id, ConversationArchive input, CancellationToken ct)
    {
        await conversations.ArchiveAsync(user.Id, id, input.DaLuuTru, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/tin-nhan")]
    public Task<AssistantResponse> Ask(long id, AskInput input, CancellationToken ct) =>
        assistant.AskAsync(user.Id, id, input, ct);
    [HttpPost("/api/ai/y-dinh")]
    public async Task<IActionResult> PredictIntent(
    [FromBody] AskInput input,
    [FromServices] LocalIntentService localIntent,
    CancellationToken ct)
    {
        var intent = await localIntent.PredictAsync(input.CauHoi.Trim(), ct);
        return Ok(new { yDinh = intent });
    }
}

[ApiController, Route("api/ai/trang-thai")]
public sealed class AssistantStatusController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var scriptPath = configuration["LocalAI:ScriptPath"];

        return Ok(new
        {
            daCauHinh = !string.IsNullOrWhiteSpace(scriptPath),
            cheDo = "LOCAL"
        });
    }
}
