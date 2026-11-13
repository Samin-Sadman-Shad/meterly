using Identity.Contracts.Services;
using MassTransit;
using Meterly.Contracts.Events;

namespace Identity.Services;

/// <summary>Publishes token-reuse alerts to RabbitMQ so the user's other sessions/clients can be notified.</summary>
public class MassTransitSecurityNotificationPublisher : ISecurityNotificationPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitSecurityNotificationPublisher> _logger;

    public MassTransitSecurityNotificationPublisher(
        IPublishEndpoint publishEndpoint, ILogger<MassTransitSecurityNotificationPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishTokenReuseDetectedAsync(
        Guid userId, Guid familyId, DateTime detectedAtUtc, bool hasOtherActiveSessions, CancellationToken cancellationToken = default)
    {
        var message = new RefreshTokenReuseDetected
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            SchemaVersion = 1,
            CorrelationId = Guid.NewGuid(),
            TraceParent = System.Diagnostics.Activity.Current?.Id,
            AggregateId = userId,
            AggregateVersion = 0, // security alert, not an aggregate state change
            UserId = userId,
            FamilyId = familyId,
            DetectedAtUtc = new DateTimeOffset(detectedAtUtc, TimeSpan.Zero),
            HasOtherActiveSessions = hasOtherActiveSessions,
            Message = "A previously used refresh token was presented again. The affected session has been revoked. "
                      + "If this was not you, change your password."
        };

        try
        {
            await _publishEndpoint.Publish(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Revocation has already happened; a broker outage must not turn the 401 into a 500.
            _logger.LogError(ex,
                "Failed to publish RefreshTokenReuseDetected for user {UserId}, family {FamilyId}", userId, familyId);
        }

        _logger.LogWarning("Refresh token reuse detected for user {UserId}, family {FamilyId}; family revoked.", userId, familyId);
    }
}
