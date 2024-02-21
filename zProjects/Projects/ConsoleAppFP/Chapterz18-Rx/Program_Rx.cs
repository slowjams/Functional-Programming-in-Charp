using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Unit = System.ValueTuple;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Net.Http;

namespace ConsoleAppFP.Chapterz18_Rx
{
    static class RatesApi
    {
        // get your own key if my free trial has expired
        const string ApiKey = "1a2419e081f5940872d5700f";

        static string UriFor(string ccyPair)
        {
            var (baseCcy, quoteCcy) = ccyPair.SplitAt(3);
            return $"https://v6.exchangerate-api.com/v6/{ApiKey}" +
               $"/pair/{baseCcy}/{quoteCcy}";
        }

        record Response(decimal ConversionRate);

        public static async Task<decimal> GetRateAsync(string ccyPair)
        {
            Console.WriteLine($"fetching rate!...");

            Task<string> request = new HttpClient().GetStringAsync(UriFor(ccyPair));

            string body = await request;

            var response = JsonSerializer.Deserialize<Response>(body, new JsonSerializerOptions());

            return response.ConversionRate;
        }       
    }

    public static class Program18
    {
        public static IDisposable Trace<T>(this IObservable<T> source, string name)
        {
            return source.Subscribe
            (
                onNext: t => Console.WriteLine($"{name} -> {t}"),
                onError: ex => Console.WriteLine($"{name} ERROR: {ex.Message}"),
                onCompleted: () => Console.WriteLine($"{name} END")
            );
        }

        //-------------------------------------------------------------------------------------------------------------------------------V
        public static (IObservable<R> Completed, IObservable<Exception> Faulted) Safely<T, R>(this IObservable<T> ts, Func<T, Task<R>> f)  
        {
            return ts.SelectMany
            (
                t => f(t).Map(Faulted: ex => ex, Completed: r => Exceptional(r))
            )   // IObservable<Exceptional<R>>
            .Partition();  // Partition method is from LaYumba.Functional.ObservableExt, which is the method below
        }

        /* 
        public static Task<R> Map<T, R>(this Task<T> task, Func<Exception, R> Faulted, Func<T, R> Completed)
         => task.ContinueWith(t =>
               t.Status == TaskStatus.Faulted
                  ? Faulted(t.Exception!)
                  : Completed(t.Result));
        
        Task.Map is from TaskExt, now you see how you can wrap System.Exception into Exceptional by using Task.ContinueWith from Task.Map
        */

        public static (IObservable<T> Successes, IObservable<Exception> Exceptions) Partition<T>(this IObservable<Exceptional<T>> excTs)
        {
            bool IsSuccess(Exceptional<T> ex) => ex.Match(_ => false, _ => true);

            T ExtractValue(Exceptional<T> ex) => ex.Match(_ => throw new InvalidOperationException(), t => t);

            Exception ExtractException(Exceptional<T> ex) => ex.Match(exc => exc, _ => throw new InvalidOperationException());

            var (ts, errs) = excTs.Partition(IsSuccess);  // ts and erros are IObservable<Exceptional<T>>

            return
            (
               Successes: ts.Select(ExtractValue),
               Exceptions: errs.Select(ExtractException)
            );
        }

        public static (IObservable<T> Passed, IObservable<T> Failed) Partition<T>(this IObservable<T> source, Func<T, bool> predicate)
        {
            return (
               Passed: source.Where(predicate),
               Failed: source.Where(predicate.Negate())
            );
        }
        //-------------------------------------------------------------------------------------------------------------------------------Ʌ
 
        public static void Main_(string[] args)
        {
            var inputs = new Subject<string>();

            var (rates, errors) = inputs.Safely(RatesApi.GetRateAsync);  // rates is IObservable<decimal>, errors is IObservable<Exception>

            IObservable<string> outputs = rates
                .Select(LaYumba.Functional.Decimal.ToString)
                .Merge(errors.Select(ex => ex.Message))
                .StartWith("Enter a currency pair like 'EURUSD', or 'q' to quit");

            using (outputs.Subscribe(Console.WriteLine))
                for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
                    inputs.OnNext(input);
        }

        //public static void Main(string[] args)
        //{
        //    var inputs = new Subject<string>();

        //    IObservable<string> rates =
        //       from pair in inputs
        //       from rate in RatesApi.GetRateAsync(pair)  // might throw an exception here
        //       select rate.ToString();   // public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)

        //    using (inputs.Trace("inputs"))
        //    using (rates.Trace("rates"))
        //        for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
        //            inputs.OnNext(input);
        //}

        public static void Main_Important_GetRateAsync_Called_Twice(string[] args)
        {
            var inputs = new Subject<string>();

            IObservable<decimal> rates =
               from pair in inputs
               from rate in RatesApi.GetRateAsync(pair)
               select rate;

            IObservable<string> outputs = from r in rates select r.ToString();

            using (inputs.Trace("inputs"))
            using (rates.Trace("rates"))
            using (outputs.Trace("outputs"))
                for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
                    inputs.OnNext(input);
        }

        public static void Main22(string[] args)
        {
            var inputs = new Subject<string>();

            IObservable<decimal> rates =
                from pair in inputs
                from rate in RatesApi.GetRateAsync(pair)
                select rate;

            IObservable<string> outputs = from r in rates select r.ToString();

            using (inputs.Trace("inputs"))
            using (rates.Trace("rates"))
            using (outputs.Trace("outputs"))
                for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
                    inputs.OnNext(input);

        }

        public static void Combind_Partition()
        {
            IObservable<decimal> rates = default!;

            IObservable<string> outputs = Observable
                .Return("Enter a currency pair like 'EURUSD', or 'q' to quit")
                .Concat(rates.Select(LaYumba.Functional.Decimal.ToString));

            // the need to provide a starting value for an IObservable is so common that there's a dedicated function for it—StartWith
            IObservable<string> outputs_ = 
                rates.Select(LaYumba.Functional.Decimal.ToString).StartWith("Enter a currency pair like 'EURUSD', or 'q' to quit");

            // whereas Concat waits for the left IObservable to complete before producing values from the right observable
            // Merge combines values from two IObservables without delay
            IObservable<decimal> rates__ = default!;
            IObservable<string> errors = default!;
            IObservable<string> outputs__ = 
                rates.Select(LaYumba.Functional.Decimal.ToString).Merge(errors);

            // partitioning a stream
            TimeSpan oneSec = TimeSpan.FromSeconds(1);
            IObservable<long> ticks = Observable.Interval(oneSec);
            var (evens, odds) = ticks.Partition(x => x % 2 == 0);

        }

        public static void Main__(string[] args)
        {
            IObservable<string> justHello = Observable.Return("Hello");
            justHello.Trace("justHello");

            // prints: justHello -> hello
            // justHello END

            Observable.FromAsync(() => RatesApi.GetRateAsync("USDEUR"))
                      .Trace("singleUsdEur");

            // prints: singleUsdEur -> 0.92
            // singleUsdEur END

            IEnumerable<char> e = new[] { 'a', 'b', 'c' };
            IObservable<char> chars = e.ToObservable();
            chars.Trace("chars");

            // prints: chars -> a
            // chars -> b
            // chars -> c
            // chars END

            TimeSpan oneSec = TimeSpan.FromSeconds(1);
            IObservable<long> ticks = Observable.Interval(oneSec);

            ticks.Select(n => n * 10)  // Select returns IObservable<long>, need to check the source code to see if it is a new one
                 .Trace("ticksX10");

            // ticksX10-> 0
            // ticksX10-> 10
            // ticksX10-> 20
            // ...
        }

        public static void Linq_Select()
        {
            var inputs = new Subject<string>();

            IObservable<decimal> rates =
                from pair in inputs
                select RatesApi.GetRateAsync(pair).Result;

            using (inputs.Trace("inputs"))
            using (rates.Trace("rates"))
                for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
                    inputs.OnNext(input);

            // eurusd
            // inputs->EURUSD
            // rates-> 1.0852
            // chfusd
            // inputs->CHFUSD
            // rates-> 1.0114
            // .. until press Q
            Console.ReadLine();
        }

        public static void Linq_SelectAsync()
        {
            var inputs = new Subject<string>();

            var rates = inputs.SelectMany(pair => Observable.FromAsync(() => RatesApi.GetRateAsync(pair)));

            using (inputs.Trace("inputs"))
            using (rates.Trace("rates"))
                for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
                    inputs.OnNext(input);

            Console.ReadLine();
        }

        public static void Linq_SelectAsyncFrom()
        {
            var inputs = new Subject<string>();

            IObservable<decimal> rates =
                from pair in inputs
                from rate in RatesApi.GetRateAsync(pair)
                select rate;

            using (inputs.Trace("inputs"))
            using (rates.Trace("rates"))
                for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
                    inputs.OnNext(input);

            Console.ReadLine();
        }

        //public static IDisposable Trace<T>(this IObservable<T> source, string name)
        //{
        //    return source.Subscribe
        //    (
        //        onNext: t => Console.WriteLine($"{name} -> {t}"),
        //        onError: ex => Console.WriteLine($"{name} ERROR: {ex.Message}"),
        //        onCompleted: () => Console.WriteLine($"{name} END")
        //    );
        //}

        public static void Main___(string[] args)
        {
            //IObservable<int> nums = default!;
            //nums.Subscribe(Console.WriteLine);

            //----------------------------------------------------V
            TimeSpan oneSec = TimeSpan.FromSeconds(1);
            IObservable<long> ticks = Observable.Interval(oneSec);

            ticks.Trace("ticks");  // the IObservable will fire/produce a value automatically when there is a subscriber

            // ticks-> 0
            // ticks-> 1
            // ticks-> 2
            //----------------------------------------------------Ʌ

            //---------------------------------------------------------V
            var inputs = new Subject<string>();

            using (inputs.Trace("inputs"))
            {
                for(string input; (input = Console.ReadLine()) != "q";)
                    inputs.OnNext(input);   // you imperatively(you tell the Subject when to fire) and this goes somewhat counter to the reactive philosophy of Rx
                                            // that's why it's recommended that you avoid Subjects
                inputs.OnCompleted();
            }
            //---------------------------------------------------------Ʌ

            //-----------------------------------------------------------------------V
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");

            IObservable<RedisValue> RedisNotifications(RedisChannel channel)
            {
                // public static IObservable<TResult> Create<TResult>(Func<IObserver<TResult>, Action> subscribe)
                return Observable.Create<RedisValue>(observer =>  // observer is IObserver<RedisValue>
                {
                    ISubscriber subscriber = redis.GetSubscriber();  // ISubscriber is defined in StackExchange.Redis
                    subscriber.Subscribe(channel, (_, val) => observer.OnNext(val));
                    return () => subscriber.Unsubscribe(channel);  // return a function that will be called when the subscription is disposed
                });                              
            }

            RedisChannel weather = "weather";

            IObservable<RedisValue> weatherUpdates = RedisNotifications(weather);
            weatherUpdates.Subscribe(onNext: val => Console.WriteLine($"It's {val} out there"));

            redis.GetDatabase(0).Publish(weather, "stormy");  // prints: It's stormy out there
            //-----------------------------------------------------------------------Ʌ

            Console.ReadLine();
        }

        #region Demo Why Chained IObservable causes multiple "calls"
        public static Random rnd = new Random();

        private static string Test2(string s)
        {
            return s + "_";
        }

        private static string Test(int i)
        {
            return i.ToString() + "_";
        }

        private static IEnumerable<int> TestX()
        {
            yield return rnd.Next();

            yield return rnd.Next();
        }

        public static void Main_Chained_IObservable_Multiple_Call(string[] args)
        {
            var temp =
                from e in TestX()
                select Test(e);

            var temp2 =
                from e in temp
                select Test2(e);

            foreach (var _ in temp)
            {
                Console.WriteLine(_);
            }

            foreach (var _ in temp2)
            {
                Console.WriteLine(_);
            }

            Console.ReadLine();

            //var inputs = new Subject<string>();

            //var (rates, errors) = inputs.Safely(RatesApi.GetRateAsync);  // rates is IObservable<decimal>, errors is IObservable<Exception>

            //IObservable<string> outputs = rates
            //    .Select(LaYumba.Functional.Decimal.ToString)
            //    .Merge(errors.Select(ex => ex.Message))
            //    .StartWith("Enter a currency pair like 'EURUSD', or 'q' to quit");

            //using (outputs.Subscribe(Console.WriteLine))
            //    for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
            //        inputs.OnNext(input);
        }
        #endregion
    }
}