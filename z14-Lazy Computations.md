Chapter 14-Lazy Computations, Monadic Composition
=========================================

## Why Laziness

Let's look at one example

```C#
T Pick<T>(T l, T r) => new Random().NextDouble() < 0.5 ? l : r;

Pick(1 + 2, 3 + 4); // => 3, or 7
```

when you invoke `Pick`, both the expressions `1 + 2` and `3 + 4` are evaluated, even though only one of them is needed in the end. Image you have a very expensive fucntion call rather than simple quick call like `3 + 4`, how do you prevent unnecessary expensive computation? You can do

```C#
T Pick<T>(Func<T> l, Func<T> r) => (new Random().NextDouble() < 0.5? l : r)();  // pick up l or r then invoke it

Pick(() => 1 + 2, () => 3 + 4); // => 3, or 7
```

note that you need to think `+` as a function that takes two parameter because `() => 1 + 2` will still get compiled into `() => 3` internally, this is just a quick demo, to see how laziness apply, you need to look at a more appropriate example that uses real expensive operations rather than integer addition as below shows:

```C#
public interface IRepository<T> 
{ 
   Option<T> Lookup(Guid id); 
}

public class CachingRepository<T> : IRepository<T>
{
   IDictionary<Guid, T> cache;
   IRepository<T> db;

   public Option<T> Lookup_Bad(Guid id)
   {
      return cache.Lookup(id).OrElse_Bad(db.Lookup(id));  // bad: expensive Lookup function always get executed even though it won't be needed in the end
   }

   public Option<T> Lookup_Good(Guid id)
   {
      return cache.Lookup(id).OrElse_Good(() => db.Lookup(id));  // good: Lookup only inovke when it is needed
   }

   public Option<T> Lookup_Also_Good(Guid id)
   {
      return cache.Lookup(id) || db.Lookup(id) ;  // also good, but you need to overload false, true and | operators
   }
}

//----------------------------------------------------------------------------->>
public static Option<T> OrElse_Bad<T>(this Option<T> left, Option<T> right)
{
   return left.Match
   (
      () => right,
      (_) => left
   );
}

public static Option<T> OrElse_Good<T>(this Option<T> opt, Func<Option<T>> fallback)
{
   return opt.Match
   (
      None: fallback,
      Some: _ => opt
   );
}

// a quick refresher
public struct Option<T> : IEquatable<NoneType>, IEquatable<Option<T>>
{
   // ...

   public R Match<R>(Func<R> None, Func<T, R> Some)
   {
      return isSome ? Some(value!) : None();
   }
}
//-----------------------------------------------------------------------------<<
```

## Overload && and || 

```C#
// a quick refresher on ternary operator ? :
bool b = ...;
string greeting = b ? "Hi" : "Bye";

// above is converted by the compiler as below:

if (!b)
{
   return "Bye";
}

return "Hi";
```

```C#
// below overload methods are in Option<T> class
// x && y is evaluated as T.false(x) ? x : T.&(x, y),  x || y is evaluated as T.true(x) ? x : T.|(x, y), where x is cache.Lookup(id) and y is db.Lookup(id)
public static bool operator true(Option<T> opt) => opt.isSome;

public static bool operator false(Option<T> opt) => !opt.isSome;

public static Option<T> operator |(Option<T> l,Option<T> r) => l.isSome ? l : r;
```

```C#
public Option<T> Lookup_Also_Good(Guid id)
{
   return cache.Lookup(id) || db.Lookup(id) ; 
}

// above method will be converted into below

public Option<T> Lookup_Also_Good(Guid id)
{
   if (!Option.true(cache.Lookup(id)))  // cache.Lookup(id) only get evaluated once, looks lke the compiler will generate optimized code to use temp variables
   {
      return cache.Lookup(id)
   }
   
   return Option.|(cache.Lookup(id), db.Lookup(id));
}
```


## Exception handling with Try

```C#
public static void Main(string[] args)
{
   //----------------------------------------------V    
   var lazyGrandma = () => "grandma";
   var turnBlue = (string s) => $"blue {s}";
   var lazyGrandmaBlue = lazyGrandma.Map(turnBlue);
            
   lazyGrandmaBlue(); // => "blue grandma"
   //----------------------------------------------Ʌ

   //----------------------------------------------V   
   CreateUri("http://github.com").Run();   // => Success(http://github.com/)       
   
   CreateUri("rubbish").Run();             // => Exception(Invalid URI: The format of the URI could not be...)

   // note that you can not directly call Try<T> as below, it will throw an unhandled exception, you need to use Run to chain it like above
   CreateUri("rubbish")();  // boom!, exception thrown
   
   Try_F(() => new Uri("http://google.com")).Run();   // you don't need to created a dedicated CreateUri function like the one above
   //----------------------------------------------Ʌ

   Try_F(() => ExtractUri_Bad("rubbish")).Run();   // use Try to rescue unsafe functions
}

public static Uri ExtractUri_Bad(string json)  // unsafe method, can throw an exception
{
   var website = JsonSerializer.Deserialize<Website>(json);
   return new Uri(website.Uri);
}

public static Try<Uri> CreateUri(string uri)  // method is defined to return Try<T>
{
    return () => new Uri(uri);
}

public static Try<T> Parse<T>(string s)  // method is defined to return Try<T>
{
    return () => JsonSerializer.Deserialize<T>(s);
}

public static Try<Uri> ExtractUri_Bind(string json)  // not very readable, use the Linq version below
{
    return Parse<Website>(json)  // Try<Website>
           .Bind(website => CreateUri(website.Uri));
}

public static Try<Uri> ExtractUri(string json)
{
    return
       from website in Parse<Website>(json)
       from uri in CreateUri(website.Uri)
       select uri;
}

public static Exceptional<Uri> CreateUri_Bad(string uri)  // bad, as you need do write try catch for every methods
{
   try { return new Uri(uri); }
   catch (Exception ex) { return ex; }
}  

public record Website(string Name, string Uri);
```

```C#
//--------------------------V
public static class FuncTExt
{
    public static Func<R> Map<T, R>(this Func<T> f, Func<T, R> g)
    {
       return () => g(f());
    }

    public static Func<R> Bind<T, R>(this Func<T> f, Func<T, Func<R>> g) 
    {
       return () => g(f())();
    }

    public static Func<R> Select<T, R>(this Func<T> @this, Func<T, R> func)
    {
       return  @this.Map(func);
    }

    public static Func<P> SelectMany<T, R, P>(this Func<T> @this, Func<T, Func<R>> bind, Func<T, R, P> project)
    {
       return () =>
       {
          T t = @this();
          R r = bind(t)();
          return project(t, r);
       };
    }
}
//--------------------------Ʌ

//--------------------------------------V
public delegate Exceptional<T> Try<T>();   // <--------------------------

public static partial class F
{
   public static Try<T> Try_F<T>(Func<T> f) => () => f();  // just name it Try_F to make it readable, you can name it Try too
}

public static class TryExt
{
   public static Exceptional<T> Run<T>(this Try<T> @try)
   {
      try 
      { 
         return @try(); 
      }
      catch (Exception ex) 
      { 
         return ex; 
      }
   }

   public static Try<R> Map<T, R>(this Try<T> @try, Func<T, R> f)
   {
      return () => @try.Run().Match<Exceptional<R>>(ex => ex, t => f(t));
   }

   public static Try<R> Bind<T, R>(this Try<T> @try, Func<T, Try<R>> f)
   {
      return () => @try.Run().Match<Exceptional<R>>(ex => ex, t => f(t).Run());
   }

   public static Try<R> Select<T, R>(this Try<T> @this, Func<T, R> func) => @this.Map(func);

   public static Try<RR> SelectMany<T, R, RR>(this Try<T> @try, Func<T, Try<R>> bind, Func<T, R, RR> project)
   {
      return 
         () => @try.Run().Match(
               ex => ex,
               t => bind(t)
                    .Run()
                    .Match<Exceptional<RR>>(ex => ex,r => project(t, r)));
   }
}
//--------------------------------------Ʌ

//---------------------------V
public static class OptionExt
{
    public static T GetOrElse<T>(this Option<T> opt, T defaultValue)
    {
        return opt.Match
        (
           None: () => defaultValue,
           Some: (t) => t
        );
    }

    public static T GetOrElse<T>(this Option<T> opt, Func<T> fallback)
    {
        return opt.Match
        (
           None: () => fallback(),
           Some: (t) => t
        );
    }

    public static Option<T> OrElse<T>(this Option<T> opt, Func<Option<T>> fallback)
    {
        return opt.Match
        (
           None: fallback,
           Some: (_) => opt
        );
    }

    public static Option<T> OrElse_Bad<T>(this Option<T> left, Option<T> right)  // to see CachingRepository example why it is bad
    {
        return left.Match
        (
           None: () => right,
           Some: (_) => left
        );
    }
}
//---------------------------Ʌ
```

To understand why the `Map` method enforces lazy computations, you need to see the compiler generated code:

```C#
//----------------------------------------------------------->>
public static Func<R> Map<T, R>(this Func<T> f, Func<T, R> g)
{
   return () => g(f());   // <--------emmm, it looks like closure, no lazy computations?
}
//-----------------------------------------------------------<<

//---------------------------------------------V
[CompilerGenerated]
private sealed class <>c__DisplayClass1_0<T, R>
{
   public Func<T, R> g;

   public Func<T> f;

   internal R <Map>b__0()
   {
      return g(f());  
   }
}

[System.Runtime.CompilerServices.NullableContext(1)]
public Func<R> Map<[System.Runtime.CompilerServices.Nullable(2)] T, [System.Runtime.CompilerServices.Nullable(2)] R>(Func<T> f, Func<T, R> g)
{
   <>c__DisplayClass1_0<T, R> <>c__DisplayClass1_ = new <>c__DisplayClass1_0<T, R>();
   <>c__DisplayClass1_.g = g;
   <>c__DisplayClass1_.f = f;
   return new Func<R>(<>c__DisplayClass1_.<Map>b__0);   // now you see why it is still lazy computations
}
//---------------------------------------------Ʌ
```


## Middleware

```C#
public delegate dynamic Middleware<T>(Func<T, dynamic> continuation);  // <-------------------------------

public static class Middleware_Ext
{
    public static Middleware<R> Bind<T, R>(this Middleware<T> mw, Func<T, Middleware<R>> f)
    {
        return continuation => mw(t => f(t)(continuation));
    }
                                                              // Func<SqlConnection, int>
    public static Middleware<R> Map<T, R>(this Middleware<T> mw, Func<T, R> f)  // T is SqlConnection, R is int
    {
        return continuation => mw(t => continuation(f(t)));  // t is SqlConnection

        // continuation is Func<int, dynamic>, which will be Func<int, dynamic> after chained with Run
    }

    public static T Run<T>(this Middleware<T> mw)  // <---------------------------
    {
        return (T)mw(t => t);
        // T is int
    }

    public static Func<T> ToNullary<T>(this Func<Unit, T> f) => () => f(Unit()); 

    public static Middleware<R> Select<T, R> (this Middleware<T> mw, Func<T, R> f)
    {
        return cont => mw(t => cont(f(t)));
    }

    public static Middleware<R> SelectMany_B<T, R>(this Middleware<T> mw, Func<T,   Middleware<R>> f)
    {
        return cont => mw(t => f(t)(cont));
    }

    public static Middleware<RR> SelectMany<T, R, RR>(this Middleware<T> @this, Func<T, Middleware<R>> f, Func<T, R, RR> project)
    {
        return continuation => @this
        (
            t => f(t)(r => continuation(project(t, r)))
        );
    }
}
```

```C#
Middleware<SqlConnection> BasicPipeline =>
   from _ in Time("InsertLog")
   from conn in Connect
   select conn;


public static Middleware<RR> SelectMany<T, R, RR>(this Middleware<T> @this, Func<T, Middleware<R>> f, Func<T, R, RR> project)
{
   //     continuation is Func<SqlConnection, int>, which is sqlConn => sqlConn.Execute("sp_create_log", message, commandType: CommandType.StoredProcedure)
   return continuation => @this  // @this is Middleware ff => Instrumentation.Time(logger, "InsertLog", ff.ToNullary()); where ff is Func<Unit, dynamic>
   (
      // t is Unit
      t => f(t)(r => continuation(project(t, r)))  // r is SqlConnection, project just returns r
      //   f(t) is Middleware<SqlConnection>
   ); 
   // t => f(t)(r => continuation(project(t, r))) the whole thing (let's call it continuationTime) is a "continuation" of Func<Unit, dynamic>
}
```

```C#
//----------------------------------->>
public static class ConnectionHelper
{
    public static R Connect<R>(string connString, Func<SqlConnection, R> func)
    {
        using var conn = new SqlConnection(connString);
        conn.Open();
        return func(conn);
    }

    public static R Transact<R>(SqlConnection conn, Func<SqlTransaction, R> f)
    {
        using var tran = conn.BeginTransaction();

        R r = f(tran);
        tran.Commit();

        return r;
    }
}
//-----------------------------------<<

//----------------------------------->>
public static class Instrumentation
{
    public static T Time<T>(ILogger logger, string op, Func<T> f)  // f is continuationTime showed as above,  where t is Unit, and the whole thing is chained with ToNullary()
    {                                                              // f is t => f(t)(r => continuation(project(t, r))), 
        var sw = new Stopwatch();                                  // so f is actually () => f(Unit())

        sw.Start();
        T t = f();
        sw.Stop();

        logger.LogDebug($"{op} took {sw.ElapsedMilliseconds}ms");

        return t;
    }

    public static T Trace<T>(ILogger logger, string op, Func<T> f)
    {
        logger.LogTrace($"Entering {op}");
        T t = f();
        logger.LogTrace($"Leaving {op}");
        return t;
    }
}
//-----------------------------------<<
```