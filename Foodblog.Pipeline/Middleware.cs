namespace Foodblog.Pipeline;

public delegate App<TEnv, TRequest, TResponse> Middleware<TEnv, TRequest, TResponse>(App<TEnv, TRequest, TResponse> next);
