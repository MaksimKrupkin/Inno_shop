namespace Shared.Infrastructure.Events;

public sealed class UserDeletedEvent
{
    public Guid UserId { get; set; }
}