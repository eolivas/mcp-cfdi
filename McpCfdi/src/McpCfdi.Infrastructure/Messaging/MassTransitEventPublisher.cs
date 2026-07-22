using MassTransit;
using McpCfdi.Application.Interfaces;
using McpCfdi.Domain.Common;

namespace McpCfdi.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events via MassTransit's publish endpoint.
/// </summary>
public sealed class MassTransitEventPublisher : IApplicationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken);
    }
}
