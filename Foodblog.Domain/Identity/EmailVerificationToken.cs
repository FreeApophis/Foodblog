namespace Foodblog.Domain.Identity;

public sealed record EmailVerificationToken(string Token, UserId UserId, DateTimeOffset ExpiresAt, bool Consumed);
