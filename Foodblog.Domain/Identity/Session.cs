namespace Foodblog.Domain.Identity;

public sealed record Session(string Token, UserId UserId, DateTimeOffset ExpiresAt);
