namespace Foodblog.Pipeline;

public delegate Effect<TEnv, TResponse> App<TEnv, TRequest, TResponse>(TRequest request);
