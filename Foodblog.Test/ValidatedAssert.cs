using Xunit.Sdk;

namespace Foodblog.Test;

public static class ValidatedAssert
{
    public static TSuccess IsSuccess<TFailure, TSuccess>(Validated<TFailure, TSuccess> validated) =>
        validated.Match(
            failure: f => throw new XunitException($"Expected Success but was Failure: {f.Error}"),
            success: s => s.Value);

    public static TFailure IsFailure<TFailure, TSuccess>(Validated<TFailure, TSuccess> validated) =>
        validated.Match(
            failure: f => f.Error,
            success: _ => throw new XunitException("Expected Failure but was Success"));
}
