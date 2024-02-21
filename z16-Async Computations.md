Chapter 16-Asynchronous Computations
=========================================

```C#
public static class CurrencyLayer  // let's say it's a paid subscriptionthat provides good quality exchange rate data
{
    public static Task<decimal> GetRateAsync(string ccyPair) => Task.FromResult<decimal>(0);  // just for demo purpose
}

public static class RatesApi  // normal services to use when the above paid service stop working
{
    public static Task<decimal> GetRateAsync(string ccyPair) => Task.FromResult<decimal>(0);  // just for demo purpose
}

//public record Response(decimal ConversionRate);
// public const string ApiKey = "1a2419e081f5940872d5700f";

public static void Main(string[] args)
{
    CurrencyLayer.GetRateAsync("EURUSD").OrElse(() => RatesApi.GetRateAsync("EURUSD"));

    RatesApi
        .GetRateAsync("EURUSD")
        .Map(rate => $"The rate is {rate}")
        .Recover(ex => $"Error fetching rate: {ex.Message}");

    RatesApi.GetRateAsync("USDEUR").Map(Faulted: ex => $"Error fetching rate: {ex.Message}", Completed: rate => $"The rate is {rate}");

    Retry(10, 1000, () => RatesApi.GetRateAsync("GBPUSD"));
}

//-----------------------------traditional approach----------------------------------------V
public static async Task<decimal> GetRateAsync_Tradi(string ccyPair)
{
    Task<string> request = new HttpClient().GetStringAsync(UriFor(ccyPair));
    string body = await request;
    var response = JsonSerializer.Deserialize<Response>(body, opts);
    return response.ConversionRate;
}

public static string UriFor(string ccyPair)
{
    var (baseCcy, quoteCcy) = ccyPair.SplitAt(3);
    return $"https://v6.exchangerate-api.com/v6/{ApiKey}" + $"/pair/{baseCcy}/{quoteCcy}";
}

public static readonly JsonSerializerOptions opts = default;
//-----------------------------------------------------------------------------------------Ʌ

public static Task<decimal> GetRateAsync_Linq(string ccyPair)
{
    return
        from body in new HttpClient().GetStringAsync(UriFor(ccyPair))
        let response = JsonSerializer.Deserialize<Response>(body, opts)  // Deserialize is not async, not good
                select response.ConversionRate;

}

public static Task<decimal> GetRateAsync(string ccyPair)
{
    return
        from str in new HttpClient().GetStreamAsync(UriFor(ccyPair))
        from response in JsonSerializer.DeserializeAsync<Response>(str, opts)  // DeserializeAsync now, good
                select response.ConversionRate;
}

public static Task<T> Retry<T>(int retries, int delayMillis, Func<Task<T>> start)
{
    return retries <= 0
        ? start()   // last attempt
        : start().OrElse(() =>
            from _ in Task.Delay(delayMillis)
            from t in Retry(retries - 1, delayMillis * 2, start)
            select t);
}
```

```C#
public static class Tasks_Traversable
{
    public static Airline jetstar = default;
    public static Airline tiger = default;

    public static void Main_() { }

    //-----------------------------------------------------------------------------------------------------V
    public static Task<Flight> BestFareSequential_NotSoGood(string origin, string dest, DateTime departure)
    {
        return
            from j in jetstar.BestFare(origin, dest, departure)
            from t in tiger.BestFare(origin, dest, departure)
            select j.Price < t.Price ? j : t;
    }

    public static async Task<RR> SelectMany_BestFareSequential_NotSoGood<T, R, RR>(this Task<T> task, Func<T, Task<R>> bind, Func<T, R, RR> project)
    {
        T t = await task;
        R r = await bind(t);  // second task only starts when first task is completed
        return project(t, r);
    }
    //-----------------------------------------------------------------------------------------------------Ʌ

    //----------------------------------------------------------------------------------------------V  those method belows need to be modified when copying to walkthrough
    public static Task<Flight> BestFareParallel_Good(string origin, string dest, DateTime departure)
    {
        Func<Flight, Flight, Flight> pickCheaper = (Flight l, Flight r) => l.Price < r.Price ? l : r;

        return
            Async(pickCheaper)
            .Apply(jetstar.BestFare(origin, dest, departure)) // first task can immediately returns
            .Apply(tiger.BestFare(origin, dest, departure));  // then second task starts to execute, so it is parallel
    }

    public static async Task<R> Apply_<T, R>(this Task<Func<T, R>> f, Task<T> arg)
    {
        return (await f)(await arg);
    }

    public static Task<Func<T2, R>> Apply__<T1, T2, R>(this Task<Func<T1, T2, R>> f, Task<T1> arg)
    {
        return Apply_(f.Map(F.Curry), arg);
    }

    public static async Task<R> Map_<T, R>(this Task<T> task, Func<T, R> f)
    {
        //=> f(await task);
        return f(await task.ConfigureAwait(false));
    }

    public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> func)
    {
        return t1 => t2 => func(t1, t2);
    }
    //----------------------------------------------------------------------------------------------Ʌ
}

//------------------------------------------------------------------------>>
public interface Airline
{
    Task<Flight> BestFare(string from, string to, DateTime on);
    Task<IEnumerable<Flight>> Flights(string from, string to, DateTime on);
}

public class Flight
{
    public decimal Price { get; set; }
}
//------------------------------------------------------------------------<<
```

## Source Code

```C#
public static partial class F
{
   public static Task<T> Async<T>(T t) => Task.FromResult(t);
}

public static class TaskExt 
{
    public static async Task<R> Select<T, R>(this Task<T> task, Func<T, R> f)   // aka Map
    {
        return f(await task);
    }
    
    public static async Task<R> Bind<T, R>(this Task<T> task, Func<T, Task<R>> f)
    {
        return await f(await task);
    }

    public static async Task<R> Map<T, R>(this Task<T> task, Func<T, R> f)   // Map plus optimization
    {
        return f(await task.ConfigureAwait(false));
    }

    public static async Task<RR> SelectMany<T, R, RR>(this Task<T> task, Func<T, Task<R>> bind, Func<T, R, RR> project)
    {
        T t = await task;
        R r = await bind(t);
        return project(t, r);
    }

    public static Task<R> Map<T, R>(this Task<T> task, Func<Exception, R> Faulted, Func<T, R> Completed)
    {
        return task.ContinueWith(t => t.Status == TaskStatus.Faulted ? Faulted(t.Exception) : Completed(t.Result));
    }

    public static async Task<R> Apply<T, R>(this Task<Func<T, R>> f, Task<T> arg)
    {
        return (await f)(await arg);
    }

    public static Task<T> OrElse<T>(this Task<T> task, Func<Task<T>> fallback)
    {
        return task.ContinueWith(t => t.Status == TaskStatus.Faulted ? fallback() : Async(t.Result))
                   .Unwrap();
    }

    public static Task<T> Recover<T>(this Task<T> task, Func<Exception, T> fallback)
    {
        return task.ContinueWith(t => t.Status == TaskStatus.Faulted ? fallback(t.Exception) : t.Result);
    }
}
```