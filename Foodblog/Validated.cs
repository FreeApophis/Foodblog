namespace Foodblog;

[DiscriminatedUnion]
public abstract partial record Validated<TFailure, TSuccess>
{
    public sealed partial record Failure(TFailure Error) : Validated<TFailure, TSuccess>;

    public sealed partial record Success(TSuccess Value) : Validated<TFailure, TSuccess>;
}

public static class Validated
{
    public static Validated<TFailure, TSuccess> Succeed<TFailure, TSuccess>(TSuccess value) =>
        new Validated<TFailure, TSuccess>.Success(value);

    public static Validated<TFailure, TSuccess> Fail<TFailure, TSuccess>(TFailure error) =>
        new Validated<TFailure, TSuccess>.Failure(error);

    public static Validated<TFailure, TResult> Apply<TFailure, TValue, TResult>(
        this Validated<TFailure, Func<TValue, TResult>> validatedFunc,
        Validated<TFailure, TValue> validatedValue,
        Func<TFailure, TFailure, TFailure> combine) =>
        (validatedFunc, validatedValue) switch
        {
            (Validated<TFailure, Func<TValue, TResult>>.Success(var f), Validated<TFailure, TValue>.Success(var v)) =>
                Succeed<TFailure, TResult>(f(v)),
            (Validated<TFailure, Func<TValue, TResult>>.Failure(var e), Validated<TFailure, TValue>.Success(_)) =>
                Fail<TFailure, TResult>(e),
            (Validated<TFailure, Func<TValue, TResult>>.Success(_), Validated<TFailure, TValue>.Failure(var e)) =>
                Fail<TFailure, TResult>(e),
            (Validated<TFailure, Func<TValue, TResult>>.Failure(var e1), Validated<TFailure, TValue>.Failure(var e2)) =>
                Fail<TFailure, TResult>(combine(e1, e2)),
            _ => throw new InvalidOperationException("Unknown Validated case"),
        };

    public static Validated<TFailure, Func<T2, TResult>> Apply<TFailure, T1, T2, TResult>(
        this Func<T1, T2, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, TResult>>>(a => b => func(a, b))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, TResult>>> Apply<TFailure, T1, T2, T3, TResult>(
        this Func<T1, T2, T3, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, TResult>>>>(a => b => c => func(a, b, c))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, Func<T4, TResult>>>> Apply<TFailure, T1, T2, T3, T4, TResult>(
        this Func<T1, T2, T3, T4, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, Func<T4, TResult>>>>>(a => b => c => d => func(a, b, c, d))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, Func<T4, Func<T5, TResult>>>>> Apply<TFailure, T1, T2, T3, T4, T5, TResult>(
        this Func<T1, T2, T3, T4, T5, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, TResult>>>>>>(a => b => c => d => e => func(a, b, c, d, e))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, TResult>>>>>> Apply<TFailure, T1, T2, T3, T4, T5, T6, TResult>(
        this Func<T1, T2, T3, T4, T5, T6, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, TResult>>>>>>>(a => b => c => d => e => f => func(a, b, c, d, e, f))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, TResult>>>>>>> Apply<TFailure, T1, T2, T3, T4, T5, T6, T7, TResult>(
        this Func<T1, T2, T3, T4, T5, T6, T7, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, TResult>>>>>>>>(a => b => c => d => e => f => g => func(a, b, c, d, e, f, g))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, TResult>>>>>>>> Apply<TFailure, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
        this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, TResult>>>>>>>>>(a => b => c => d => e => f => g => h => func(a, b, c, d, e, f, g, h))
            .Apply(value, combine);

    public static Validated<TFailure, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, Func<T9, TResult>>>>>>>>> Apply<TFailure, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(
        this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func,
        Validated<TFailure, T1> value,
        Func<TFailure, TFailure, TFailure> combine) =>
        Succeed<TFailure, Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, Func<T9, TResult>>>>>>>>>>(a => b => c => d => e => f => g => h => i => func(a, b, c, d, e, f, g, h, i))
            .Apply(value, combine);
}
