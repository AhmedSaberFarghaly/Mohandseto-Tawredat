using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Application.AdminMarketing;

public sealed record MarketingSendResult(bool Accepted, string? ProviderReference, string? Error);

public interface IMarketingChannelSender
{
    Task<MarketingSendResult> SendAsync(MarketingCampaignChannel channel, string destination, string title,
        string body, string? actionUrl, CancellationToken ct);
}

/// <summary>
/// Development transport. Production can replace this registration with email and WhatsApp providers without
/// changing campaign targeting, scheduling, or delivery auditing.
/// </summary>
public sealed class ConsoleMarketingChannelSender(ILogger<ConsoleMarketingChannelSender> logger) : IMarketingChannelSender
{
    public Task<MarketingSendResult> SendAsync(MarketingCampaignChannel channel, string destination, string title,
        string body, string? actionUrl, CancellationToken ct)
    {
        var reference = $"console-{Guid.NewGuid():N}";
        logger.LogInformation("Marketing {Channel} accepted for {Destination}; reference {Reference}", channel, destination, reference);
        return Task.FromResult(new MarketingSendResult(true, reference, null));
    }
}

