Chapter 6-`Map`, `Bind` Patterns in FP
==============================

## Mapping a Function onto a Sequence

An implementation of `Map` for `IEnumerable`

```C#
public static IEnumerable<R> Map<T, R>(this IEnumerable<T> ts, Func<T, R> f)
{
   foreach (var t in ts)
      yield return f(t);
}
```
```C#
var triple = (int x) => x * 3;

Range(1, 3).Map(triple);   // => [3, 6, 9]
```

this is exactly the behavior you get when you call LINQ's `Select` method:

```C#
public static IEnumerable<R> Map<T, R>(this IEnumerable<T> ts, Func<T, R> f) {
   return ts.Select(f);
}
```

The signature of `Map` for `IEnumerable` is:
`(IEnumerable<T>, (T -> R)) -> IEnumerable<R>`


## Mapping a Function onto an `Option`

To get the signature of Map for `Option`, simply follow the pattern and replace `IEnumerable` with `Option`:
`(Option<T>, (T -> R)) -> Option<R>`

```C#
public static Option<R> Map<T, R>(this Option<T> optT, Func<T,R> f) {
   return optT.Match
   (
      () => None,
      (t) => Some(f(t))
   );
}
```
```C#
string greet = (string name) => $"hello, {name}";

Option<string> empty = None;
Option<string> optJohn = Some("John");

empty.Map(greet); // => None
optJohn.Map(greet); // => Some("hello, John")
```

`Option` abstracts away the question of whether a value is present or not. If you directly apply a function to a value, you have to somehow ensure that the value is available. Instead, if you Map that function onto an Option, you don't really care whether the value is there or not, `Map` applies the function or not, as appropriate. 

In chapter 4, we defined a function that would calculate `Risk` based on `Age` as:

```C#
public struct Age 
{
   private int Value { get; }

   public static Option<Age> Create(int age)
   {
      return IsValid(age) ? Some(new Age(age)) : None;
   }

   private Age(int value) {   // constructor is private now
      Value = value;
   }

   private static bool IsValid(int age) => 0 <= age && age < 120;
}

public Risk CalculateRiskProfile(Age age) {
   return (age < 60) ? Risk.Low : Risk.Medium;
}
```

Now, assume you're doing a survey where people volunteer some personal information and receive some statitics, Survey takers are modeled with a `Subject` class, Some fields like Age are modeled as optional because survey takers can choose whether or not to disclose this information:

```C#
public rec൦rd Subject(
   Option<Age> Age,
   Option<Gender> Gender,
   // many more fields...
)
```

This is how you'd compute the Risk of a particular Subject:

```C#
public Option<Risk> RiskOf(Subject subject) {
   return subject.Age.Map(CalculateRiskProfile);
}
```


## Introducing Functors 

In FP, a type for which such a `Map` function is defined is called a functor. `IEnumerable` and `Option` are functors, as you've just seen. 

The signature of Map can be generalized as follows:
`Map : (C<T>, (T -> R)) -> C<R>`

For practical purpuse, we can say that anything that has a reasonable implementation of `Map` is a functor. But what's a reasonable implementation? Essentially, `Map` should apply a function to the container's inner value(s) and, equally important, it should do nothing else: **`Map` should have no side effects**.


You might want to add an functor interface, after all, if both Option and IEnumerable support the Map operation, why are we not capturing this with an interface? Unfortunately, it is not possible in C#. To illustrate why, let's try to define such an interface:

```C#
interface Functor<W<>, T> {   // W means wrapper
   W<R> Map<R>(Func<T, R> f);
}

// code doesn't compile
public struct Option<T> : Functor<Option, T> 
{
   public Option<R> Map<R>(Func<T, R> f) => // ...
}

// we can't use F<> as a type variable in C#, it is feasible in other languages like Haskell and Scala
```

## Performing Side Effects with `ForEach`

In last chapter, we talked about `Action` and `Function`, now let's think about how can we use `Map` with `Action`.

Let's start with basic, we know `List<T`> has a `ForEach` method that takes an `Action<T>`:

```C#
new List<int> { 1, 2, 3 }.ForEach(Console.Write);   // prints: 123
```

Let's generalize this so we can call `ForEach` on any `IEnumerable`:

```C#
public static IEnumerable<Unit> ForEach<T>(this IEnumerable<T> ts, Action<T> action)
{
   return ts.Map(action.ToFunc()).ToImmutableList();
}
```

So you want to apply an `Action` on `IOption<T>`, so the first thing that comes into your mind is probably to add an overload of `Map`:

```C#
public static Option<R> Map<T, R>(this Option<T> optT, Func<T, R> f)
{
   return optT.Match(
      () => None,
      (t) => Some(f(t))
   );
}

public static Option<Unit> Map<T>(this Option<T> optT, Action<T> f)
{
   // ...
}
```
the compiler fails to resolve to the right overload when we call Map without specifying its generic arguments. The reason for this is fairly technical: overload resolution doesn't take into account output parameters, so it can't distinguish between `Func<T, R>` and `Action<T>` when it comes to overload resolution. The price to pay for such an overload would be to always specify generic arguments when calling Map, again causing noise. In short, the best solution is to introduce a dedicated ForEach method.

<div class="alert alert-info p-1" role="alert">
    I don't know why the author said we can't have an overload here. I still add an overload version of Map that takes Action in the souce code section
</div>

Now, let's see the definition of `ForEach` ("overload" `Map`) for Option. This is defined trivially in terms of `Map`, using the `ToFunc` function that converts an `Action` into a `Func`:

```C#
public static Option<Unit> ForEach<T>(this Option<T> opt, Action<T> action) 
{
   return Map(opt, action.ToFunc());
} 
```
The ForEach name can be slightly counterintuitive—remember, an Option has at most one inner value, so the given action is invoked exactly once (if the Option is Some) or never (if it's None). Here's an example of using ForEach to apply an Action to an Option:

```C#
var opt = Some("John");

opt.ForEach(name => Console.WriteLine($"Hello {name}"));   // prints: Hello John
```

However, remember from chapter 3 that we should aim to separate pure logic from side effects. **We should use `Map` for logic and `ForEach` for side effects**, so it would be
preferable to rewrite the preceding code as follows:

```C#
opt.Map(name => $"Hello {name}")
   .ForEach(Console.WriteLine);
```


## Chaining Functions with Bind

Sometimes we can't use `Map`, for example:
```C#
// Int.Parse : string -> Option<int>
// Age.Create : int -> Option<Age>

string input = Prompt("Please enter your age:");

Option<int> optI = Int.Parse(input);
Option<Option<Age>> ageOpt = optI.Map(i => Age.Create(i));

// so we get Option<Option<Age>>, not Option<Age> we want
```

So we introduce `Bind` to fix this issue. Here is the signature of `Bind`:
`Option.Bind : (Option<T>, (T -> Option<R>)) -> Option<R>`

```C#
public static class OptionExtension
{
   public static Option<R> Bind<T, R>(this Option<T> optT, Func<T, Option<R>> f)
   {
      return optT.Match(
         () => None,
         (t) => f(t)   // use f(t) directly, not a Some(f(t)) wrapper compared to Map below
      );
   }

   public static Option<R> Map<T, R>(this Option<T> optT, Func<T, R> f)
   {
      return optT.Match(
         () => None,
         (t) => Some(f(t))
      );
   }
}
```

Now we can convert `Option<int>` to `Option<Age>`

```C#
Func<string, Option<Age>> parseAge = s => Int.Parse(s).Bind(Age.Create);

string input = Prompt("Please enter your age:");

Option<int> optI = Int.Parse(input);
Option<Age> ageOpt = optI.Bind(i => Age.Create(i));
```

## Flattening Nested Lists with `Bind`

You've seen how ou can use `Bind` to avoid having nested `Options` (`Option<Option<Age>>`). The same idea applies to lists. 

```C#
public static class IEnumerableExtension
{
    public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> f)   // similiar to LINQ's SelectMany
    {
       foreach(T t in ts) 
       {
          foreach(R r in f(t)) 
          {
             yield return r;
          }
       }
    }
    
    public static IEnumerable<R> Map<T, R>(this IEnumerable<T> ts, Func<T, R> f)
    {
       return ts.Select(f);
    }
}
```

Let's look at an example:

```C#
using Pet = System.String;

public class Neighbor {
   public Neighbor(string Name, IEnumerable<Pet> Pets) {
      // ...
   }
}

var neighbors = new Neighbor[] {
   new (Name: "John", Pets: new Pet[] {"Fluffy", "Thor"}),
   new (Name: "Tim", Pets: new Pet[] {}),
   new (Name: "Carl", Pets: new Pet[] {"Sybil"}),
};

IEnumerable<IEnumerable<Pet>> nested = neighbors.Map(n => n.Pets);   // => [["Fluffy", "Thor"], [], ["Sybil"]]

IEnumerable<Pet> flat = neighbors.Bind(n => n.Pets);   // => ["Fluffy", "Thor", "Sybil"]
```

Let's now generalize the pattern for Bind:

`Bind : (C<T>, (T -> C<R>)) -> C<R>`

Actually, it's called a *monad*. 
as we know functors are types for which a suitable `Map` function is defined, enabling you to apply a function to the functor’s inner value(s).
Similarly, **monads** are types for which a `Bind` function is defined, enabling you to effectively combine two (or more) monad-returning functions without ending up with a
nested structure.


## Filtering values with Where

```C#
public static Option<T> Where<T>(this Option<T> optT, Func<T, bool> pred) 
{
   return optT.Match
   (
      () => None,
      (t) => pred(t) ? optT : None   // if true, return itself
   )
}
```
```C#
public static class Int {
   public static Option<int> Parse(string s) => int.TryParse(s, out int result) ? Some(result) : None;
}

bool IsNatural(int i) => i >= 0;
Option<int> ToNatural(string s) => Int.Parse(s).Where(IsNatural);

ToNatural("2");     // => Some(2)
ToNatural("-2");    // => None
ToNatural("hello")  // => None
```


## Combining `Option` and `IEnumerable` with Bind

One way to look at `Option` is as a special case of a list that can either be empty (None) or contain exactly one value (Some). You can express this in code by making it possible to convert an `Option` into an `IEnumerable` as:

```C#
public struct Option<T> 
{
   public IEnumerable<T> AsEnumerable() {
      if (isSome)
         yield return value!;  
   }
}
```

let's go back to the example of a survey:

```C#
public class Subject {
   // ...
   public Subject(Option<Age> Age)
}

IEnumerable<Subject> Population => new[]
{
   new Subject(Age.Create(33)),
   new Subject(None),           // this person did not disclose their age
   new Subject(Age.Create(37)),
};
```

Now suppose you need to compute the average age of the participants. How can you go about that? let's say you start by selecting all the values for Age:

```C#
IEnumerable<Option<Age>> optionalAges = Population.Map(p => p.Age);   
// => [Some(Age(33)), None, Some(Age(37))]
```

## Using `Bind` to Get Deisred Type and Filter None

```C#
public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> list, Func<T, Option<R>> func)
{
   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, Option<R>> f)   // straightforward implementation
   {
      foreach (T t in ts)
      {
         foreach (R r in f(t).AsEnumerable())   // if Option<R> (`f(t).AsEnumerable()`) is None, then does nothing, this is how you can filter out None
         {
            yield return r;
         }
      }
   } 

   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> list, Func<T, Option<R>> func)
   {
      ts.Bind(t => f(t).AsEnumerable());  // Bind is the function below
   }
   

   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> f)
   {
      foreach(T t in ts)
      {
         foreach(R r in f(t))
         {
            yield return r;
         }
      }
   }

   //---------------------------
   public static IEnumerable<R> Bind<T, R>(this Option<T> opt, Func<T, IEnumerable<R>> f)
   {
      return opt.AsEnumerable().Bind(f);  // Bind is the function above
   }
}
```
```C#
public class Subject {
   // ...
   public Subject(Option<Age> Age)
}

IEnumerable<Subject> Population => new[]
{
   new Subject(Age.Create(33)),
   new Subject(None),           
   new Subject(Age.Create(37)),
};

IEnumerable<Option<Age>> optionalAges = Population.Map(p => p.Age);  // => [Some(Age(33)), None, Some(Age(37))]

IEnumerable<Age> Ages = Population.Bind(p => p.Age);   // => [Age(33), Age(37)]
```

`Bind : (IEnumerable<Subject>, (Subject -> Option<Age>)) -> IEnumerable<Age>`

So you see how we can use `Bind` to get non-nested result and filter out `None`. To filter out `None` is very convenient as you will use quite often and see in the exercise in the end of this chapter. Let's see how `Bind` filter out `None` so let's start check the `Bind` method and `Option` code in details:

```C#
public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, Option<R>> f) 
{
   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, Option<R>> f)   
   {
      foreach (T t in ts)
      {
         foreach (R r in f(t).AsEnumerable())  // <----A
         {
            yield return r;
         }
      }
   } 
}

public struct Option<T>
{
   private readonly T? value;
   private readonly bool isSome;

   internal Option(T value)
   {
      this.value = value;
      this.isSome = true;
   }

   public IEnumerable<T> AsEnumerable()
   {
      if (isSome)   // <----------------------B
         yield return value!;
   }

   // ...

   public R Match<R>(Func<R> None, Func<T, R> Some) => isSome ? Some(value!) : None();
}
```
You can see that in `Option<T>`, if `f(t)` is `None`, then `isSome` is false, now you should get the idea :)


## Source Code:

```C#
public static class MapExtension
{
   public static IEnumerable<R> Map<T, R>(this IEnumerable<T> ts, Func<T, R> f)
   {
      return ts.Select(f);
   }

   public static Option<R> Map<T, R>(this Option<T> optT, Func<T, R> f)
   {
      return optT.Match(
         () => None,
         (t) => Some(f(t))
      );
   }

   public static Option<Unit> ForEach<T>(this Option<T> opt, Action<T> action) 
   {
      return Map(opt, action.ToFunc());
   } 

   // author says this overload will requre us specify type when using it, I don't know why. It is basically the same as above one
   public static Option<Unit> Map<T>(this Option<T> optT, Action<T> action)
   {
      return Map(optT, action.ToFunc());  
   }

   // other extension methods
   public static Option<T> Where<T>(this Option<T> optT, Func<T, bool> pred)   // so the returning Option<T> contains None when 1. optT itself is None; 2. pred(t) is false
   {
      return optT.Match
      (
         () => None,
         (t) => pred(t) ? optT : None
      );
   }
}

public static class BindExtension
{
   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, Option<R>> f)
   {
      foreach (T t in ts)
      {
         foreach (R r in f(t).AsEnumerable())
         {
            yield return r;
         }
      }
      
      // or
      // ts.Bind(t => f(t).AsEnumerable());
   }

   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> f)
   {
      foreach (T t in ts)
      {
         foreach (R r in f(t))
         {
            yield return r;
         }
      }
   }

   public static IEnumerable<R> Bind<T, R>(this Option<T> opt, Func<T, IEnumerable<R>> f)
   {
      return opt.AsEnumerable().Bind(f);
   }

   public static Option<R> Bind<T, R>(this Option<T> optT, Func<T, Option<R>> f)  // T and R could be the same, check the example in Chapter 7's Option<AccountState>.Bind example
   {
      return optT.Match(
         () => None,
         (t) => f(t)
      );
   }
}
```


## Exercises

```C#
public record Employee(string Id, Option<WorkPermit> WorkPermit, DateTime JoinedOn, Option<DateTime> LeftOn);
```
```C#
public record WorkPermit(string Number, DateTime Expiry);
```

1. Use Bind and an Option-returning Lookup function to implement `GetWorkPermit` so that `GetWorkPermit` returns None if the work permit has expired.

```C#
// helper methods
public static Option<T> Lookup<K, T>(this IDictionary<K, T> dict, K key) => dict.TryGetValue(key, out T? value) ? Some(value) : None;

public static Func<WorkPermit, bool> HasExpired => permit => permit.Expiry < DateTime.Now.Date;
// <

public static Option<WorkPermit> GetWorkPermit(Dictionary<string, Employee> employees, string employeeId) 
{
   Option<Employee> employee = employees.Lookup(employeeId);
   Option<WorkPermit> result = employee.Bind(e => e.WorkPermit);

   return result.Where(w => !HasExpired(w))
}
```

2. Use Bind to implement AverageYearsWorkedAtTheCompany. Only employees who have left should be included.

```C#
// helper methods
public static double YearsBetween(DateTime start, DateTime end) => (end - start).Days / 365d;

public static double AverageYearsWorkedAtTheCompany(List<Employee> employees)
{
   IEnumerable<double> result employees.Bind(e => e.LeftOn.Map(leftOn => YearsBetween(e.JoinedOn, leftOn)));
   return result.Average();
}
```

<!-- <div class="alert alert-info p-1" role="alert">
    
</div> -->

<!-- ![alt text](./zImages/17-6.png "Title") -->

<!-- <code>&lt;T&gt;</code> -->

<!-- <div class="alert alert-info pt-2 pb-0" role="alert">
    <ul class="pl-1">
      <li></li>
      <li></li>
    </ul>  
</div> -->

<!-- <ul>
  <li><b></b></li>
  <li><b></b></li>
  <li><b></b></li>
  <li><b></b></li>
</ul>  -->

<!-- <span style="color:red">hurt</span> -->

<style type="text/css">
.markdown-body {
  max-width: 1800px;
  margin-left: auto;
  margin-right: auto;
}
</style>

<link rel="stylesheet" href="./zCSS/bootstrap.min.css">
<script src="./zCSS/jquery-3.3.1.slim.min.js"></script>
<script src="./zCSS/popper.min.js"></script>
<script src="./zCSS/bootstrap.min.js"></script>