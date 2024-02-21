Chapter 5-Modeling the Possible Absence of Data
==============================

Let's start with an example that shows bad APIs you use every day:

```C#
string emptyColor = new NameValueCollection()["color"];  // color doesn't exist in the appsetting files, return null so it is dishonest

var alsoEmptyColor = new Dictionary<string, string>();
string color = alsoEmptyColor["color"];  // throws KeyNotFoundException, therefore it is dishonest
```
both of indexers (indexers are, of course, just normal functions, the [] syntax is just sugar) are `dishonest`


## An introduction to the `Option` type

`Option` is essentially a container that wraps a value or no value. Without showing the source code of FP library, let's get familiar with its usage first:

```C#
public class OptionTutorial 
{
   static void Main(string[] args) 
   {
      Option<string> _ = None;
      Option<string> john = Some("John");

      string s1 = Greet(Some("John"));   // => "Hello, John"
      string s2 = Greet(None);           // => "Sorry, who?"
   }

   static string Greet(Option<string> greetee)
   {
      return greetee.Match(
         None: () => "sorry, who?",
         Some: (name) => $"Hello, {name}");
   }
}
```

 ## Implementing Option Step by Step

```C#
public interface Option<T> { }    // just a marker interface
public record None : Option<T>;   // doesn't compile
public record Some<T>(T Value) : Option<T>;
```
because `None` does not actually contain a T, we'd like to say that None is a valid `Option<T>` regardless of what type T eventually resolves to. Unfortunately, the C# compiler does not allow this, so in order to make the code compile, we need to provide a generic parameter for None as well:

```C#
interface Option<T> { }    // marker interface
record None<T> : Option<T>; 
record Some<T>(T Value) : Option<T>;
```

then we want to consume an `Option` using pattern matching:

```C#
// code compiles but it is not elegant
string Greet(Option<string> greetee) 
{   
   return greetee switch                 
   {                                      // issue one: compiler warning: "the switch expression does not handle all possible values of its input type"
      None<string> => "Sorry, who?",      // issue two: we have to include the type name `string`, imagine we have a long type name rather than simple string type here :(
      Some<string>(var name) => $"Hello, {name}"
   };
}
```

We have a comiler warning here because in theory, some other implementations of `Option<string>` could exist and there is no way to tell the compiler we never want anything other than `Some` and `None` to implement `Option`.

We can use *discard pattern* to mitigate both issues:

```C#  
static R Match<T, R>(this Option<T> opt, Func<R> None, Func<T, R> Some) {
   return opt switch {
      None<T> => None(),
      Some<T>(var t) => Some(t),
      _ => throw new ArgumentException("Option must be None or Some")
   };
}
```
then we can let methods take an `Option` as parameters:
```C#
string Greet(Option<string> greetee) {
   return greetee.Match(
      None : () => "Sorry, who?",
      Some : (name) => $"Hello, {name}"
   );
}
```

## Creating a None

so in the client code's perspective, if we want to create a `None` and use it, we do:
```C#
var greeting = Greet(new None<string>());
```
emmmm, doesn't look nice, we don't like the fact that we have to specify the string type in the angle brackets. What we need is a value that can be converted to a `None<T>`, regardless of the type of `T` and we can't do this with inheritance but type conversion:

```C#
struct NoneType { }

public abstract record Option<T> {
   public static implicit operator Option<T>(NoneType _) {
      return new None<T>();
   }
}
```

This effectively tells the runtime that an instance of NoneType can be used where an `Option<T>` is expected and instructs the runtime to convert the NoneType to a`None<T>`. Finally, we include a convenience field called None that stores a `NoneType`:

```C#
public static readonly NoneType None = default;

string s = Greet(None);   // => "Sorry, who?"
```
much better :)


## Creating a Some

```C#
public record Some<T> : Option<T> 
{
   private T Value { get; }

   public Some (T value) {
      Value = value ?? throw new ArgumentNullException();
   }

   public void Deconstruct(out T value) {
      value = Value;
   }
}
```
look like `Nullable<T>`, isn't it?

We can then define a convenience static function, `Some`, that wraps a given value into a `Some` and we can put this static method somewhere else e.g. a static help class:

```C# 
public static Option<T> Some<T>(T t) => new Some<T>(t);  // we can put this method in a static helper class, consuming client can do `using static XXX` to access it

string s = Greet(Some("John")) // => "Hello, John"
```
Now we have nice, clean syntax for creating both a None and a Some. To put the icing on the cake, I'm also going to define an implicit conversion from `T `to `Option<T>`:

```C#
public abstract record Option<T>
{
   public static implicit operator Option<T>(NoneType _) => new None<T>();

   public static implicit operator Option<T>(T value) => value is null ? new None<T>() : new Some<T>(value);
}

public record None<T> : Option<T>;

public record Some<T> : Option<T>
{
   private T Value { get; }

   public Some(T value) => Value = value ?? throw new ArgumentNullException();

   public void Deconstruct(out T value) => value = Value;
}

public struct NoneType { }
```
```C#
Greet("John")   // => "Hello, John"

Option<string> emptyColor = _config["color"];  // emptyColor is None
```

## Optimizing the Option Implementation

For a number of reasons, in the `LaYumba.Functional` library, I've chosen to use a slightly different approach and define `Option` as in the following:

```C#
namespace LaYumba.Functional
{
   using static F;

   public static partial class F
   {
      public static Option<T> Some<T>([NotNull] T? value) // NotNull: `value` is guaranteed to never be null if the method returns without throwing an exception
         => new(value ?? throw new ArgumentNullException(nameof(value)));

      public static NoneType None => default;
   }

   public struct NoneType {}

   public struct Option<T>
   {
      readonly T? value;
      readonly bool isSome;

      internal Option(T value)
      {
         this.value = value ?? throw new ArgumentNullException();
         this.isSome = true;
      }

      public IEnumerable<T> AsEnumerable()
      {
         if (isSome)
            yield return value!;
      }

      public static implicit operator Option<T>(NoneType _) => default;

      // public static implicit operator Option<T>(T value) => value is null ? None : new Option<T>(value);  // if value is null, there are two operation of "overloads" which includes this one and the one above, 

      public static implicit operator Option<T>(T value) => value is null ? default : new Option<T>(value);  // not sure why the author choose the above one, maybe just for demo purpose, since None is more meaningful than default

      public R Match<R>(Func<R> None, Func<T, R> Some) => isSome ? Some(value!) : None();
   }
}
```

Note that this optimized `Option<T>` implementation has some advantages:

* `Option<T>` is struct now, performance is better because structs are allocated on the stack.
* Being a struct, an `Option<T>` cannot be null
* There is only one type `Option<T>` to represent `Some<T>` and `None<T>`

Let's look how we can fix the problem shown in the begining of the chapter:

```C#
new NameValueCollection()["color"]          // => null
new Dictionary<string, string>()["color"]   // => runtime error: KeyNotFoundException
```

We can fix it very easily:

```C#
public static Option<string> Lookup(this NameValueCollection collection, string key) {
   return collection[key];
} 

public static Option<T> Lookup<K, T>(this IDictionary<K, T> dict, K key) {
   return  dict.TryGetValue(key, out T value) ? Some(value) : None;
}

new NameValueCollection().Lookup("green")         // => None
new Dictionary<string, string>().Lookup("blue")   // => None
```

## The Smart Constructor Pattern

We can improve the `Age` type from previous chapter by using a pattern called **smart constructor**. It is smart in the sense that it's aware of some rules and can prevnent the constructor throws an exception when you have wrong input:

```C#
public struct Age 
{
   private int Value { get; }

   public static Option<Age> Create(int age)
   {
      return IsValid(age) ? Some(new Age(age)) : None;
   }

   private Age(int value)  // <-----------------constructor is private now
   {   
      Value = value;
   }

   private static bool IsValid(int age) => 0 <= age && age < 120;

   public static bool operator <(Age l, Age r) => l.Value < r.Value;
   
   public static bool operator >(Age l, Age r) => l.Value > r.Value;
   
   public static bool operator <(Age l, int r) => l < new Age(r);
   
   public static bool operator >(Age l, int r) => l > new Age(r);
}
```

If you now need to obtain an Age from an int, you'll get an `Option<Age>`, which forces you to account for the failure case. 


## Why `null` is Bad

You may have heard that the NullReferenceException is the single most common source of bugs. But why is it so common? The answer lies, I believe, in a fundamental ambiguity:

* Because reference types are `null` by default, your program may encounter a `null` as a result of a programming logic error, where a required value was simply not initialized.
* Other times, null is considered a legal value; for example, the authors of `NameValueCollection` decided it was OK to represent that a key is not present by returning `null`.

Because there is no way to declare whether a null value is deliberate or the result of a programming logic error  (at least before C# 8’s nullable reference types), you're often in doubt as to how to treat a null value. Should you allow for `null`? Should you throw an `ArgumentNullException`?

The ambiguity between legal and unintentional nulls does not only cause bugs. It has another effect, which may be even more damaging: it leads to defensive programming. How many times you see developers doing `if (xxx == null)` to pollute the code base?


## Gaining Robustness by using Option Instead of Null

The main step to address these problems is to never use null as a legal value. Instead, use `Option` to represent optional va.lues. This way, any occurrence of `null` is the result
of a programming error. (This means that you never need to check for null; just let the NullReferenceException bubble up) 

Imagine you have a form on your website that allows people to subscribe to a newsletter. A user enters his name and email, and this causes the instantiation of a Subscriber, which is then persisted to the database. Subscriber is defined as follows:

```C#
public class Subscriber {
   public string Name {get; set;}
   public string Email {get; set;}
}

public string GreetingFor(Subscriber subscriber) => $"Dear {subscriber.Name.ToUpper()},";
```
This all works fine. Name can't be null because it's a required field in the signup form, and it's not nullable in the database.

Some months later, the rate at which new subscribers sign up drops, so the business decides to lower the barrier to entry by no longer requiring new subscribers to enter their name. The name field is optional from the form, and the database is modified accordingly. Then you could make the Name property mullable , but to use C# 8's Nullable feature, you have to turn it on in the csproj file on add `#nullable enable` in the begining of the class file, for example:
```C#
#nullable enable

public class Subscriber 
{
   public string Name? {get; set;}
   public string Email {get; set;}
}
```
well, unless you're treating warnings as errors, your code will still compile, and bring a lot of warinngs you might just get tired of it and ignore them eventually. And compiler is not smart enough to turn off the warning, for example:

```C#
public string GreetingFor(Subscriber subscriber) 
{
   return IsValid(subscriber)   // check that if subscriber.Name is null
      ? $"Dear {subscriber.Name.ToUpper()},"   // compiler still generate the warning even though you have already done the null check 
      : "Dear Subscriber";
}
```

That's how `Option<T>` comes into rescure:

```C#
public class Subscriber {
   public Option<string> Name {get; set;}
   public string Email {get; set;}
}

public string GreetingFor(Subscriber subscriber) {
   return subscriber.Name.Match(
      () => "Dear Subscriber,",
      (name) => $"Dear {name.ToUpper()},"
   );
}
```
This not only clearly conveys the fact that a value for Name may not be available, it causes GreetingFor to no longer compile, which force you to modify every place that `Subscriber.Name` is accessed.


## Source Code

```C#
public static partial class F   // used by client to assit them create Option<T> in a more convenient way
{
   public static Option<T> Some<T>([NotNull] T? value) => new Option<T>(value ?? throw new ArgumentNullException(nameof(value)));
   public static NoneType None => default;  // the reason why we need this static method and NoneType is to assist users so they don't need to do `default(Option<string>)`
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
      if (isSome)
         yield return value!;
   }

   public static implicit operator Option<T>(NoneType _) => default;

   public static implicit operator Option<T>(T value) => value is null ? default : new Option<T>(value);  // that's how you can do `Option<string> str = null`
   
   public R Match<R>(Func<R> None, Func<T, R> Some)   // note that return type is R , not not necessarily T
   {
      return isSome ? Some(value!) : None();
   }
}
public struct NoneType { }
```