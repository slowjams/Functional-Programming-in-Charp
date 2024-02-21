using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using Boc.Domain.Events;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using ConsoleAppFP.Model;

namespace ConsoleAppFP.Chapterz16_Async
{
    public static class Program16
    {
        public record Response(decimal ConversionRate);
        public const string ApiKey = "1a2419e081f5940872d5700f";
        
        public static void Main_16(string[] args)
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

    }

    //--------------------------------V
    public static partial class F_
    {
        public static Task<T> Async<T>(T t) => Task.FromResult(t);  // t can be a Func
    }

    public static class TaskExt   // need to reorder when copy to walkthrough
    {
        public static async Task<R> Apply<T, R>(this Task<Func<T, R>> f, Task<T> arg)
        {
            return (await f)(await arg);
        }

        public static async Task<R> Bind<T, R>(this Task<T> task, Func<T, Task<R>> f)
        {
            return await f(await task);
        }

        public static async Task<R> Select<T, R>(this Task<T> task, Func<T, R> f)   // aka Map
        {
            return f(await task);
        }

        public static async Task<RR> SelectMany<T, R, RR>(this Task<T> task, Func<T, Task<R>> bind, Func<T, R, RR> project)
        {
            T t = await task;
            R r = await bind(t);
            return project(t, r);
        }

        public static async Task<R> Map<T, R>(this Task<T> task, Func<T, R> f)   // Map plus optimization
        {
            return f(await task.ConfigureAwait(false));
        }

        public static Task<R> Map<T, R>(this Task<T> task, Func<Exception, R> Faulted, Func<T, R> Completed)
        {
            return task.ContinueWith(t => t.Status == TaskStatus.Faulted ? Faulted(t.Exception) : Completed(t.Result));
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
    //--------------------------------Ʌ
}