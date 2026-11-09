using DauChanDiSan.Api.Features.Trips.DTOs;

namespace DauChanDiSan.Api.Features.Trips;

public interface IAssistantProvider
{
    bool IsConfigured { get; }
    Task<GeminiResult> AskAsync(string prompt, CancellationToken ct);
}
