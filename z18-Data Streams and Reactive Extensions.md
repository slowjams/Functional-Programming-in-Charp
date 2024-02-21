Chapter 18-Data Streams and Reactive Extensions
=========================================

* Observables produce values
* Observers consume values

```C#
//-----------------------------V
public interface IObservable<T>  // included in  standard .net framework
{
   IDisposable Subscribe(IObserver<T> observer);
}

public interface IObserver<T>   // included in  standard .net framework
{
   void OnNext(T value);
   void OnError(Exception error);
   void OnCompleted();
}
//-----------------------------Ʌ

//--------------------------------------V
public static class ObservableExtensions
{
   // public static IDisposable Subscribe<T>(this IObservable<T> source) { ... }  // evaluate the observable sequence for its side-effects only

   public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext);

   public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action onCompleted);

   public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError);
  
   public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted);
}
//--------------------------------------Ʌ
```


## Creating IObservables

The `IObservable` and `IObserver` interfaces are included in .NET Standard, but if you want to create or perform many other operations on `IObservables`, you'll typically use the Reactive Extensions (Rx) by installing the `System.Reactive` package, below is some example:

```C#
public static void Main(string[] args)
{
    IObservable<int> nums = default!;
    nums.Subscribe(Console.WriteLine);

    //----------------------------------------------------V
    TimeSpan oneSec = TimeSpan.FromSeconds(1);
    IObservable<long> ticks = Observable.Interval(oneSec);

    ticks.Trace("ticks");  // the IObservable will fire/produce a value automatically when there is a subscriber

    // ticks-> 0
    // ticks-> 1
    // ticks-> 2
    //----------------------------------------------------Ʌ

    //---------------------------------------------------------V
    var inputs = new Subject<string>();  // A Subject is an IObservable (and also an IObserver) you can imperatively tell to produce a value can consume it too

    using (inputs.Trace("inputs"))
    {
        for (string input; (input = Console.ReadLine()) != "q";)
            inputs.OnNext(input);   // you imperatively(you tell the Subject when to fire) and this goes somewhat counter to the reactive philosophy of Rx
                                    // that's why it's recommended that you avoid Subjects
        inputs.OnCompleted();
    }
    //---------------------------------------------------------Ʌ

    //---------------------------------------------------------V
    IObservable<string> justHello = Observable.Return("Hello");  // you should get the idea of what `Observable.Return` which is to produce a value then complete the stream
    justHello.Trace("justHello");
   
    // prints: justHello -> hello
    // justHello END
    //---------------------------------------------------------Ʌ

    //-----------------------------------------------------------V
    IEnumerable<string> strings = new[] { "Hi", "There", "Bye" };
    IObservable<string> stringsObs = e.ToObservable();
    stringsObs.Trace("string");

    // prints: string -> Hi
    // string -> There
    // string -> Bye
    // string END
    //-----------------------------------------------------------Ʌ

    TimeSpan oneSec = TimeSpan.FromSeconds(1);
    IObservable<long> ticks = Observable.Interval(oneSec);   // Observable.Interval produces a sequence of 0, 1, 2, increment by 1 in every second

    ticks.Select(n => n * 10)  // Select returns IObservable<long>, need to check the source code to see if it is a new one
         .Trace("ticksX10");

    // ticksX10-> 0
    // ticksX10-> 10
    // ticksX10-> 20
    // ...
}

//------------------------------------------------------------------------->>
public static IDisposable Trace<T>(this IObservable<T> source, string name)
{
    return source.Subscribe
    (
        onNext: t => Console.WriteLine($"{name} -> {t}"),
        onError: ex => Console.WriteLine($"{name} ERROR: {ex.Message}"),
        onCompleted: () => Console.WriteLine($"{name} END")
    );
}
//-------------------------------------------------------------------------<<
```

## Combining and Partitioning Streams

```C#
public static void Main(string[] args)
{
    //------------------------------------V
    IObservable<decimal> rates = ...;
    IObservable<string> outputs = Observable
       .Return("Enter a currency pair like 'EURUSD', or 'q' to quit")
       .Concat(rates.Select(LaYumba.Functional.Decimal.ToString));
    //------------------------------------Ʌ
    
    //------------------------------------V
    // the need to provide a starting value for an IObservable is so common that there's a dedicated function for it—StartWith
    IObservable<string> outputs_ = 
       rates.Select(LaYumba.Functional.Decimal.ToString).StartWith("Enter a currency pair like 'EURUSD', or 'q' to quit");
    //------------------------------------Ʌ

    //------------------------------------V
    // whereas Concat waits for the left IObservable to complete before producing values from the right observable
    // Merge combines values from two IObservables without delay
    IObservable<decimal> rates = ...;
    IObservable<string> errors = ...;
    IObservable<string> outputs = rates.Select(LaYumba.Functional.Decimal.ToString).Merge(errors);
    //------------------------------------Ʌ

    //---------------------------------------V
    // partitioning a stream
    TimeSpan oneSec = TimeSpan.FromSeconds(1);
    IObservable<long> ticks = Observable.Interval(oneSec);
    var (evens, odds) = ticks.Partition(x => x % 2 == 0);
    //---------------------------------------Ʌ
}

//---------------------------------------------------------------------------------------------------------------------------->>
public static (IObservable<T> Passed, IObservable<T> Failed) Partition<T>(this IObservable<T> source, Func<T, bool> predicate)
{
   return (
      Passed: source.Where(predicate),
      Failed: source.Where(predicate.Negate())
   );
}
//----------------------------------------------------------------------------------------------------------------------------<<
```

## Error handling with IObservable

```C#
static class RatesApi
{
    static string UriFor(string ccyPair)
    {
        var (baseCcy, quoteCcy) = ccyPair.SplitAt(3);
        return $"https://v6.exchangerate-api.com/v6/{ApiKey}" + $"/pair/{baseCcy}/{quoteCcy}";
    }

    public static async Task<decimal> GetRateAsync(string ccyPair)
    {
        Console.WriteLine($"fetching rate!...");

        Task<string> request = new HttpClient().GetStringAsync(UriFor(ccyPair));

        string body = await request;  // <---------------------- might throw an exception here if the ccPari based url is not valid

        var response = JsonSerializer.Deserialize<Response>(body, new JsonSerializerOptions());

        return response.ConversionRate;
    }
}

public static void Main(string[] args)
{
    var inputs = new Subject<string>();

    IObservable<string> rates =
       from pair in inputs
       from rate in RatesApi.GetRateAsync(pair)  // might throw an exception here
       select rate.ToString();   // public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)

    using (inputs.Trace("inputs"))
    using (rates.Trace("rates"))
        for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
            inputs.OnNext(input);
}

/*
chfusd  // <-----------input
inputs -> CHFUSD
rates -> 1.0114

xxx     // <-----------input
inputs -> XXX
rates ERROR: Input string was not in a correct format

chfusd
inputs -> CHFUSD
                     // <---------------- once a stream has an error, it never signal again
eurusd
inputs -> EURUSD

*/
```

To prevent your system from going into such a state, where a branch of the dataflow dies while the remaining graph keeps functioning, you can use the techniques you learned for functional error handling as below:

```C#
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

//-------------------------------------------------------------------------------------------------------------------------------V
public static (IObservable<R> Completed, IObservable<Exception> Faulted) Safely<T, R>(this IObservable<T> ts, Func<T, Task<R>> f)
{
    return ts.SelectMany  // SelectMany returns IObservable<Exceptional<R>>
    (
        t => f(t).Map(Faulted: ex => ex, Completed: r => Exceptional(r))  // Task.Map is from TaskExt, now you see how you can wrap System.Exception into Exceptional 
    )                                                                     // by using Task.ContinueWith from Task.Map
    .Partition();  // Partition method is from LaYumba.Functional.ObservableExt, which is the method below
}

/*
public static Task<R> Map<T, R>(this Task<T> task, Func<Exception, R> Faulted, Func<T, R> Completed)
 => task.ContinueWith(t =>
       t.Status == TaskStatus.Faulted
          ? Faulted(t.Exception!)
          : Completed(t.Result));

public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector);
*/

public static (IObservable<T> Successes, IObservable<Exception> Exceptions) Partition<T>(this IObservable<Exceptional<T>> excTs)
{
    bool IsSuccess(Exceptional<T> ex) => ex.Match(_ => false, _ => true);

    T ExtractValue(Exceptional<T> ex) => ex.Match(_ => throw new InvalidOperationException(), t => t);

    Exception ExtractException(Exceptional<T> ex) => ex.Match(exc => exc, _ => throw new InvalidOperationException());

    var (ts, errs) = excTs.Partition(IsSuccess);  // ts and errors are IObservable<Exceptional<T>>

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
//--------------------------------------------------------------------------------------------------------------------------------Ʌ
```

## Detecting Sequences of Pressed Keys

say you want to implement some behavior when the user presses the combination Alt-K-B, the traditional approach leads to a "stateful" solution which is too complicated. With Rx, you can solve this with a elegant solution, below is some prerequisite and the solution

```C#
var keys = new Subject<ConsoleKeyInfo>();

IObservable<IObservable<ConsoleKeyInfo>> demoObservable = keys.Select(_ => keys);  // <--------------------not being used but it is crucial important to understand it

for (ConsoleKeyInfo key; (key = Console.ReadKey()).Key != ConsoleKey.Q;)
    keys.OnNext(key);

/*
let's say you press a, b, c, d, e,  demoObservable will be like

when a pressed, demoObservable generates a new IObservable<ConsoleKeyInfo> instance which yields a, b, c, d, e, |
when b pressed, demoObservable generates a new IObservable<ConsoleKeyInfo> instance which yields b, c, d, e, |
...
*/

// now you do
IObservable<IObservable<ConsoleKeyInfo>> demoObservable = keys.Select(_ => keys.Take(1));
/*
when a pressed, demoObservable generates a new IObservable<ConsoleKeyInfo> which yields a, b, |
when b pressed, demoObservable generates a new IObservable<ConsoleKeyInfo> which yields b, c, |
*/

IObservable<(ConsoleKeyInfo First, ConsoleKeyInfo Second)> twoKeyCombinations =
   from first  in keysAlt
   from second in keysAlt.Take(halfSec).Take(1)
   select (First: first, Second: second);
/*
twoKeyCombinations will produce (a, b), (b, c), (c, d), (d, e), |
*/
```

Solution:

```C#
public static void _Main(string[] args)
{
    Console.WriteLine("Enter some inputs to push them to 'inputs', or 'q' to quit");

    var keys = new Subject<ConsoleKeyInfo>();
    var halfSec = TimeSpan.FromMilliseconds(500);

    IObservable<ConsoleKeyInfo> keysAlt = keys.Where(key => key.Modifiers.HasFlag(ConsoleModifiers.Alt));

    var twoKeyCombis =
        from first  in keysAlt
        from second in keysAlt.Take(halfSec).Take(1)
        select (First: first, Second: second);

    IObservable<Unit> altKB =
        from tpl in twoKeyCombis
        where tpl.First.Key == ConsoleKey.K && tpl.Second.Key == ConsoleKey.B
        select Unit();

    using (keys.Select(k => Environment.NewLine + k.KeyChar).Trace("keys"))
    using (twoKeyCombis.Select(ks => $"{ks.Item1.KeyChar}-{ks.Item2.KeyChar}").Trace("twoKeyCombis"))
    using (altKB.Trace("altKB"))
        for (ConsoleKeyInfo key; (key = Console.ReadKey()).Key != ConsoleKey.Q;)
            keys.OnNext(key);

}
```

## Reacting to Multiple Event Sources

```C#
public static void Main(string[] args)  
{
    IObservable<decimal> balance = ...;
    IObservable<decimal> eurUsdRate = ...;

    // CombineLatest works but it produces too much data becuase exchange rate changes quite often :(
    var balanceInUsd = balance.CombineLatest(eurUsdRate, (bal, rate) => bal * rate);  

    //------------------------------------------V Use `Observable.Sample` to setup a "timer" to specify interval you want to singal
    TimeSpan tenMins = TimeSpan.FromMinutes(10);
    IObservable<long> sampler = Observable.Interval(tenMins); // sampler doesn't need to be IObservable<long>, can be any IObservable<T> such as IObservable<string>
    IObservable<decimal> eurUsdSampled = eurUsdRate.Sample(sampler);  // what it matter is sampler should "tick" to notify the source, it can tick any value like 0,1,2 or a, b, c
    IObservable<decimal> balanceInUsdLessFrequent = balance.CombineLatest(eurUsdSampled, (bal, rate) => bal * rate);
    //don't know why the author use sampler in this way, it could have been much more easier as:
    IObservable<decimal> eurUsdSampled = eurUsdRate.Sample(sampler, TimeSpan.FromMinutes(10)); 
    IObservable<decimal> balanceInUsdLessFrequent = balance.CombineLatest(eurUsdSampled, (bal, rate) => bal * rate);
    //------------------------------------------Ʌ

    //-----------------------------------------------V
    IObservable<Transaction> transactions = ...;
    decimal initialBalance = 0;
    IObservable<decimal> balanceAggregated = transactions.Scan(initialBalance, (bal, trans) => bal + trans.Amount);

    // you need this because you want to signal when the current balance is negative, and you don't want to signal again when previous balance is already negative
    IObservable<Unit> dipsIntoTheRed =
        from bal in balanceAggregated.PairWithPrevious()
        where bal.Previous >= 0 && bal.Current < 0
        select Unit();
    //-----------------------------------------------Ʌ

    //------------------------------------------------V real world system needs to process transactions for all accounts, so we must group accountID
    IObservable<Transaction> transactions = ...;

    IObservable<Guid> dipsIntoRed = transactions
        .GroupBy(t => t.AccountId)  // IObservable<IGroupedObservable<Guid, Transaction>>
        .Select(DipsIntoTheRed)     // IObservable<IObservable<Guid>>
        .MergeAll();
    //------------------------------------------------Ʌ
}

//------------------------------------------------------------------------------------------------>>
public static IObservable<(T Previous, T Current)> PairWithPrevious<T>(this IObservable<T> source)
{
    return
        from first in source
        from second in source.Take(1)
        select (Previous: first, Current: second);
}

public static IObservable<T> MergeAll<T>(this IObservable<IObservable<T>> source) => source.SelectMany(x => x);
//------------------------------------------------------------------------------------------------<<
```


## Source Code (need to rework after reading CLR via C#)

```C#
//------------------------------------V
public static partial class Observable   // some of methods belong to other partical class but will list all of them here
{
   public static IObservable<long> Interval(TimeSpan period);  // produces a sequence of 0, 1, 2 (as reference for how many times it occurs I guess) by a interval of e.g every 3s
   public static IObservable<TResult> FromAsync<TResult>(Func<Task<TResult>> functionAsync);
   public static IObservable<TResult> Create<TResult>(Func<IObserver<TResult>, Action> subscribe);
   public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector);
   public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector);
   public static IObservable<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate);
   public static IObservable<IGroupedObservable<TKey, TSource>> GroupBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector);
   public static IObservable<TSource> Concat<TSource>(this IObservable<TSource> first, IObservable<TSource> second);
   public static IObservable<TSource> Merge<TSource>(this IObservable<TSource> first, IObservable<TSource> second);
   public static IObservable<TSource> StartWith<TSource>(this IObservable<TSource> source, params TSource[] values);
   public static IObservable<TSource> Take<TSource>(this IObservable<TSource> source, TimeSpan duration);  // this can be confusing, it is not you want to have a delay to consume  
   public static IObservable<TSource> Take<TSource>(this IObservable<TSource> source, int count);
   public static IObservable<TSource> Sample<TSource>(this IObservable<TSource> source, TimeSpan interval);
   public static IObservable<TSource> Sample<TSource, TSample>(this IObservable<TSource> source, IObservable<TSample> sampler);
   public static IObservable<(TFirst First, TSecond Second)> CombineLatest<TFirst, TSecond>(this IObservable<TFirst> first, IObservable<TSecond> second);
   public static IObservable<TAccumulate> Scan<TSource, TAccumulate>(this IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator);
   public static (IObservable<T> Passed, IObservable<T> Failed) Partition<T>(this IObservable<T> source, Func<T, bool> predicate);
}
//------------------------------------Ʌ

//-----------------------------------------------------------------------------------------------------------V
public abstract class SubjectBase<T> : ISubject<T>, ISubject<T, T>, IObserver<T>, IObservable<T>, IDisposable
{
   protected SubjectBase();

   public abstract bool HasObservers { get; }
   public abstract bool IsDisposed { get; }

   public abstract void Dispose();
   public abstract void OnCompleted();
   public abstract void OnError(Exception error);
   public abstract void OnNext(T value);
   public abstract IDisposable Subscribe(IObserver<T> observer);
}

public sealed class Subject<T> : SubjectBase<T>  // Subject<T> is both of IObserver<T> and IObservable<T>
{
   public Subject();
   public override bool HasObservers { get; }
   public override bool IsDisposed { get; }
   public override void Dispose();
   public override void OnCompleted();
   public override void OnError(Exception error);
   public override void OnNext(T value);
   public override IDisposable Subscribe(IObserver<T> observer);
}
//-----------------------------------------------------------------------------------------------------------Ʌ

//-----------------------------------V
public abstract class ObserverBase<T> : IObserver<T>, IDisposable
{
   private int _isStopped;

   protected ObserverBase()
   {
      _isStopped = 0;
   }

   public void OnNext(T value)
   {
      if (Volatile.Read(ref _isStopped) == 0)
      {
         OnNextCore(value);
      }
   }

   protected abstract void OnNextCore(T value);

   public void OnError(Exception error)
   {
      if (Interlocked.Exchange(ref _isStopped, 1) == 0)
      {
          OnErrorCore(error);
      }
   }

   protected abstract void OnErrorCore(Exception error);

   public void OnCompleted()
   {
      if (Interlocked.Exchange(ref _isStopped, 1) == 0)
      {
         OnCompletedCore();
      }
   }

   protected abstract void OnCompletedCore();

   internal bool Fail(Exception error)
   {
       if (Interlocked.Exchange(ref _isStopped, 1) == 0)
       {
           OnErrorCore(error);
           return true;
       }

       return false;
   }

   public void Dispose()
   {
      Dispose(true);
      GC.SuppressFinalize(this);
   }

   protected virtual void Dispose(bool disposing)
   {
      if (disposing)
      {
         Volatile.Write(ref _isStopped, 1);
      }
   }
}

public sealed class AnonymousObserver<T> : ObserverBase<T>
{
   private readonly Action<T> _onNext;
   private readonly Action<Exception> _onError;
   private readonly Action _onCompleted;

   public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
   {
       _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
       _onError = onError ?? throw new ArgumentNullException(nameof(onError));
       _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
   }

   public AnonymousObserver(Action<T> onNext) : this(onNext, Stubs.Throw, Stubs.Nop) { }  // ...

   protected override void OnNextCore(T value) => _onNext(value);

   protected override void OnErrorCore(Exception error) => _onError(error);

   protected override void OnCompletedCore() => _onCompleted();

   internal ISafeObserver<T> MakeSafe() => new AnonymousSafeObserver<T>(_onNext, _onError, _onCompleted);
}
//-----------------------------------Ʌ
```