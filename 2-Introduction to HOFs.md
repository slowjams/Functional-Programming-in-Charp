Chapter 2-Introduction to HOFs
==============================

There are several language constructs in C# that you can use to represent functions:
* Methods
* Delegates
* Dictionaries

HOFs are:

* *take other functions as inputs*
or
* *return a function as output*

Some examples of HOFs:
```C#
// Linq.Where is a HOF
public static IEnumerable<T> Where<T> (this IEnumerable<T> ts, Func<T, bool> predicate) 
{
   foreach (T t in ts) {
      if (predicate(t))
         yield return t;
   }
}
```
```C#
public static class HOFs 
{
   public static Func<T2, T1, R> SwapArgs<T1, T2, R>(this Func<T1, T2, R> f) => (t1, t2) => f(t2, t1);   // C# 9 allows you use extension method on delegate
}

var divideInt = (int x, int y) => x / y;
divideInt(10, 2) // => 5
var divideIntSwap = divideInt.SwapArgs();
divideIntSwap(2, 10) // => 5

var divideDouble = (double x, double y) => x / y;
var divideByDoubleSwap = divideDouble.SwapArgs();
// ...
```
```C#
Func<int, bool> isMod(int n) => i => i % n == 0;   // this HOF takes static data and return a function

var _ = Enumerable.Range(1, 20).Where(isMod(2))    // => [2, 4, 6, 8, 10, 12, 14, 16, 18, 20]
var _ = Enumerable.Range(1, 20).Where(isMod(3))    // => [3, 6, 9, 12, 15, 18]
```

## Using HOFs to avoid duplication

How many times you have seen `using` in the code base? for example:

```C#
using Dapper;

public class DbLogger
{
   string connString;
   
   public void Log(LogMessage msg) {
      using (var conn = new SqlConnection(connString)) {
         conn.Execute("sp_create_log", msg, commandType: CommandType.StoredProcedure);
      }
   }

   public IEnumerable<LogMessage> GetLogs(DateTime since) 
   {
      var sql = "SELECT * FROM [Logs] WHERE [Timestamp] > @since";

      using (var conn = new SqlConnection(connString)) 
      {
         return conn.Query<LogMessage>(sql, new {since = since});
      }
   }
   // the `using` goes on and on
   // public IEnumerable<XXX> GeXXX(...)  {  using (var conn = new SqlConnection(connString)) {...} } 
}
```
HOF comes into rescure:
```C#
public static class ConnectionHelper
{
    public static R Connect<R>(string connString, Func<IDbConnection, R> f)
    {
        using (var conn = new SqlConnection(connString))
        {
            conn.Open();
            return f(conn);
        }
    }
}
//---------------------------------------------------
using Dapper;

public class DbLogger 
{
   string connString;

   public void Log(LogMessage message) => Connect(connString, conn => conn.Execute("sp_create_log", message, commandType: CommandType.StoredProcedure));
   
   public IEnumerable<LogMessage> GetLogs(DateTime since) 
   {
      string sql = @"SELECT * FROM [Logs] WHERE [Timestamp] > @since";
      public IEnumerable<LogMessage> GetLogs(DateTime since) => Connect(connString, conn => conn.Query<LogMessage>(sql, new {since = since}));     
   }
}
```

## Purity and Side Effects

**side effects** are:

* Mutates global state: Global here means any state that's visible outside of the function's scope. For example, a private instance field is considered global because it's visible from all methods within the class

* Mutates its input arguments

* Throws exception

* Performs any I/O operation


**Pure functions** are:

* Output depends entirely on the input arguments
* Cause no side effects

**Impure functions** are:

* Factors other than input arguments can affect the ouput
* Can cause side effects

Note that **a function that has no side effects can still be impure**. For example, a function that reads from global mutable state is likely to have an output that depends on factors other than its inputs. **A function whose ouput depends entirely on its inputs can also be impure**, it could still have side effects such as updating global mutable state.

## Strategies for Nanaging Side Effects

* Avoid mutating arguments

```C#
// bad code, mutates linesToDelete as a side effect
decimal RecomputeTotal(Order order, List<OrderLine> linesToDelete) {
   var result = 0m;
   foreach (var line in order.OrderLines)
   if (line.Quantity == 0) 
      linesToDelete.Add(line);
   else 
      result += line.Product.Price * line.Quantity;
   return result;
}
```

The reason why this is such a bad design is that the behavior of the method is now tightly coupled with the caller: caller relies on the method to perform its side effect, and the callee relies on the caller to initialize the list, which means each method must be aware of the implementation details of the other, making it impossible to reason about the code in isolation.

```C#
// good code, avoid side effect by returning all the computed information (as Tuple) to the caller instead
(decimal NewTotal, IEnumerable<OrderLine> LinesToDelete) RecomputeTotal(Order order) {
   return order.OrderLines.Sum(line => line.Product.Price * line.Quantity), order.OrderLines.Where(line => line.Quantity == 0));
}
```

* Isolating I/O effects for better Unit Test 

Imagine you're writing some code for an online banking application. Your client is the Bank of Codeland (BOC). Let's assume that the user's request to make a transfer is represented by a MakeTransfer command. A command is a simple data transfer object (DTO) that the client sends the server. The app has two validations:

1. The Date field, representing the date on which the transfer should be executed, should not be past.

2. The BIC code, a standard identifier for the beneficiary's bank, should be valid.

Let's first see a bad design:

```C#
// bad code
public abstract rec൦rd Command(DateTime Timestamp);

public rec൦rd MakeTransfer(Guid DebitedAccountId, 
                           string Beneficiary, 
                           string Iban, 
                           string Bic, 
                           DateTime Date, 
                           decimal Amount, 
                           string Reference, 
                           DateTime TimeStamp = default) : Command(TimeStamp)
{
    public static MakeTransfer Dummy => new(default, default!, default!, default!, default!, default!, default!);
}

public interface IDateTimeService   // <--------------------------- bad, introduce "header interface" which is not what interfaces were initially designed for
{
    DateTime UtcNow { get; }
}

public class DefaultDateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}

//----------------------------V
public interface IValidator<T>
{
    bool IsValid(T t);
}

public class BicFormatValidator : IValidator<MakeTransfer>
{
    static readonly Regex regex = new Regex("^[A-Z]{6}[A-Z1-9]{5}$");
    public bool IsValid(MakeTransfer transfer) => regex.IsMatch(transfer.Bic);
}

public class DateNotPastValidator : IValidator<MakeTransfer>
{
    
    // bad, introduce global state, make `IsValid` method impure, but it is impure not because `IsValid` update the state,
    // (in fact `IsValid` only read it) but becuase `IsValid` not just depends on transfer object, it also depends on dateService
    private readonly IDateTimeService dateService;  // <--------------------------------------------bad
                                                     
    public DateNotPastValidator(IDateTimeService dateService)
    {
        this.dateService = dateService;
    }

    public bool IsValid(MakeTransfer transfer) => dateService.UtcNow.Date <= transfer.Date.Date;   // IsValid` depends on dateService, not just transfer object
}
//----------------------------Ʌ


//-----------------------------------V
public class DateNotPastValidatorTest
{
   static DateTime presentDate = new DateTime(2021, 3, 12);
   
   private class FakeDateTimeService : IDateTimeService
   {
      public DateTime UtcNow => presentDate;
   }

   [Test]
   public void WhenTransferDateIsPast_ThenValidationFails()
   {
      var svc = new FakeDateTimeService();
      var sut = new DateNotPastValidator(svc);
      var transfer = MakeTransfer.Dummy with { Date = presentDate.AddDays(-1) };
      
      Assert.AreEqual(false, sut.IsValid(transfer));
   }
}
//-----------------------------------Ʌ

// DI related
public void ConfigureServices(IServiceCollection services) {
   services.AddTransient<IDateTimeService, DefaultDateTimeService>();
   services.AddTransient<DateNotPastValidator>();
}
```

Let's see a good design:

```C#
// interally the record syntax sugar still create a get/init property, so it still store Clock fucntion as "global state", I think this example is not a good one to explain
// why global state is bad, the only benefit of this approach is: you don't need to define header interface such as `IDateTimeService`
public record DateNotPastValidator(Func<DateTime> Clock) : IValidator<MakeTransfer>   
{                                                                                                                                                                                                                                                           
    public bool IsValid(MakeTransfer transfer) => Clock().Date <= transfer.Date.Date;
}

public class DateNotPastValidatorTest
{
   private readonly DateTime today = new(2021, 3, 12);
   
   [Test]
   public void WhenTransferDateIsPast_ThenValidationFails()
   {
      var sut = new DateNotPastValidator(() => today);
      var transfer = MakeTransfer.Dummy with {
        Date = today.AddDays(-1)
      };

      Assert.AreEqual(false, sut.IsValid(transfer));
   }
}

// DI related
public void ConfigureServices(IServiceCollection services) {
   services.AddSingleton<DateNotPastValidator>(sp => new DateNotPastValidator(() => DateTime.UtcNow.Date));
}
```

As you can see, this approach effectively pushed the side effect of reading the current date outward to the code instantiating the validator.