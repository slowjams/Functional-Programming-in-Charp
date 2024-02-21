Chapter 10-Working Effectively with Multi-argument Functions
==============================

```C#
Func<int, Func<int, int>> multiply = (int x) => (int y) => x * y;  // curried form

Option<Func<int, int>> temp = Some(3).Map(multiply);  
                                                     
//------------------------------------------------------------------>>
public static Option<R> Map<T, R>(this Option<T> optT, Func<T, R> f)  // R can also be Func, so T is int, and R is Func<int, int> in this example
{
   return optT.Match
   (
      () => None,
      (t) => Some(f(t))
   );
}
//------------------------------------------------------------------<<
```

Note that `temp` is `Some(y => 3 * y))`, which you can call it an elevated function (`Option<T>` where T is a fucntion). There's nothing special about an elevated function, Functions are values, so it's simply another value wrapped in one of the usual "containers".

You can provide a non-curried form of multiply, you can still use `Map` as:

```C#
Func<int, int, int> multiply = (int x, int y) => x * y;   // non-curried form

Option<Func<int, int>> temp = Some(3).Map(multiply);  // Map will convert non-curried form to curried form for you, temp is `Some(y => 3 * y))` as above

//---------------------------------------------------------------------------------------->>
public static Option<Func<T2, R>> Map<T1, T2, R>(this Option<T1> opt, Func<T1, T2, R> func)  //  takes care of currying for client who can supply non-curried function
{
    return opt.Map(func.Curry());  // func.Curry() return Func<T1, Func<T2, R>>
}

public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> func)
{
   return t1 => t2 => func(t1, t2);
}
//----------------------------------------------------------------------------------------><<
```

So how we use `Option<Func<int, int>>` to get `Option<int>` as a result?

```C#
// Approach One, not very clean :(

Func<int, Func<int, int>> multiply = (int x, int y) => x * y;  // will be covered into below by the Map
//Func<int, Func<int, int>> multiply = (int x) => (int y) => x * y; 

Option<int> optX = Some(3);
Option<int> optY = Some(4);

Option<int> result = optX.Map(multiply).Match  // Map will curry it first as explained above
(               // = Option<Func<int, int>> temp.Match(...)
   () => None,
   (f) => optY.Match   // f is called f, not t, x or anything else, now you get the idea
   (
      () => None,
      (y) => Some(f(y))
   )
);  // result is Some(12)

// this approach is not very clean as you need to call `Match` to provide some boilerplate code
// can we abstract this and make the code consise?
```

let's see a good approach that uses `Apply`:

```C#
Func<int, int, int> multiply = (int x, int y) => x * y; 

Option<int> result = 
   Some(3).Map(multiply)    // Option<Func<int, int>>
          .Apply(Some(4));  // result is Some(12)

//------------------------------------------------------------------------------>>
public static Option<R> Apply<T, R>(this Option<Func<T, R>> optF, Option<T> optT)  // R can be Func<...>
{
   return optF.Match  // start with optF
   (
      () => None,
      (f) => optT.Match
      (
         () => None,
         (t) => Some(f(t))
      )
   );
}

public static Option<Func<T2, R>> Apply<T1, T2, R>(this Option<Func<T1, T2, R>> optF, Option<T1> arg)
{
   return Apply(optF.Map(F.Curry), arg);  // <--------------- A
}
//------------------------------------------------------------------------------<<
```


## Lifting Functions then use `Apply`

Given by the examples above such as `Some(3).Map(multiply)`, we can natually think: why not lift the fucntion directly first then "Apply" arguments?

```C#
Func<int, Func<int, int>> multiply = (int x) => (int y) => x * y; 
//Func<int, int, int> multiply = (int x, int y) => x * y;

Option<int> result 
   = Some(multiply)   // Option<Func<int, Func<int, int>>>
     .Apply(Some(3))       // first Apply will convert non-curried form of function in the second line to curried function, see A section
     .Apply(Some(4));

// note that because of implicit converion defined in Option<T>, you can also do
Option<int> result 
   = Some(multiply)   // Option<Func<int, Func<int, int>>>
     .Apply(3)        // Option<Func<int, int>>
     .Apply(4);       // Option<int>

//------------------------------------------------------------------------------>>
// Apply(3) where R is Func<int, int> and Apply(4) where R is int, both invoke this function
public static Option<R> Apply<T, R>(this Option<Func<T, R>> optF, Option<T> optT);
{
   return optF.Match  // start with optF
   (
      () => None,
      (f) => optT.Match
      (
         () => None,
         (t) => Some(f(t))  // `Some(Func<int, int>)` for `Apply(3)`, `Some(int>)` for `Apply(4)`
      )
   );
}
//------------------------------------------------------------------------------<<
```

let's put two approachs together

```C#
// first approach, lift value first :(
Some(3)
   .Map(multiply)
   .Apply(Some(4))

// second approach, lift function first :)
Some(multiply)
   .Apply(Some(3))
   .Apply(Some(4))
```

Obviously, the second approach is more readable and more intuitive


## Functors, applicatives, and monads

```C#
public static Option<R> Map<T, R>(this Option<T> opt, Func<T, R> f)
{
   return Some(f).Apply(opt);
}

public static Option<R> Bind<T, R>(this Option<T> optT, Func<T, Option<R>> f)
{
   return optT.Match(() => None, (t) => f(t));
}

public static Option<R> Apply<T, R>(this Option<Func<T, R>> optF, Option<T> optT)
{
   return optF.Match
   (
      () => None,
      (f) => optT.Match
      (
         () => None,
         (t) => Some(f(t))
      )
   );
}

public static Option<R> Map_ImplementedUsingApply<T, R>(this Option<T> opt, Func<T, R> f)
{
   return Some(f).Apply(opt);
}

public static Option<R> Apply_ImplementedUsingBind<T, R>(this Option<Func<T, R>> optF, Option<T> optT)
{
   return optT.Bind(t => optF.Bind(f => Some(f(t))));  // start witn optT
                                                       // return optT.Bind(t => optF.Apply(t)); 
}
```

You can see that `Map` is the most general and `Apply` is more powerful than `Map`, because you can use `Apply` to implement `Map`. `Bind` is more powerful than `Apply` as you can use `Bind` to implement `Apply`. 


## The Monad Laws

```C#
//  Right identity----------------V
Option<int> opt = Some(3);

Option<int> opt2 = opt.Bind(Some);

// opt == opt2 == Some(3)
//--------------------------------Ʌ

// Right identity exampleA--------------------V
Func<int, Option<int>> exp = x => Some(x * x);
Option<int> r1 = Some(3).Bind(exp);
// exp(3) = r1 = Some(9)
//--------------------------------------------Ʌ

// Right identity exampleA--------------------V
Func<int, Option<int>> exp = x => Some(x * x);
Option<int> r1 = Some(3).Bind(exp);
// exp(3) = r1 = Some(9)
//--------------------------------------------Ʌ

// Right identity exampleB-----------------------V
Func<int, IEnumerable<int>> f = i => Range(0, i);
int t = 3;
IEnumerable<int> r3 = List(t).Bind(f);
//  f(t) = result
//-----------------------------------------------Ʌ

// Left identity------------------V
Func<int, IEnumerable<int>> f = i => Range(0, i);

bool isSame = f(3) == List(t).Bind(f);  // true, they are same
//--------------------------------Ʌ

// Associativity---------------------------------------------------------------V
Func<double, Option<double>> safeSqrt = d => d < 0 ? None : Some(Math.Sqrt(d));

Option<string> m = Some("4");
Option<double> r4 = m.Bind(Double.Parse)
                     .Bind(safeSqrt);

Option<double> r5 = m.Bind(x => Double.Parse(x).Bind(safeSqrt));

// r4 = r5 = Some(2)
//-----------------------------------------------------------------------------Ʌ

//--------------------------->>
public static partial class F
{
   public static IEnumerable<T> List<T>(params T[] items) => items.ToImmutableList();
}
//---------------------------<<
```


## Functional Linq

```C#
//-------------------------------V
Option<int> result1 =  // Some(24)
   from x in Some(12)
   select x * 2;

Option<int> result2 =  // None
   from x in (Option<int>)None
   select x * 2;

bool isSame = (from x in Some(1) select x * 2) == Some(1).Map(x => x * 2);  // true

// The reson you can use Linq query expression is Option<T> has an extension method which compiler can detect and call, just like IEnumerable<T>.Select(...)

public static Option<R> Select<T, R>(this Option<T> @this, Func<T, R> func)
{
   return @this.Map(func);
}
//-------------------------------Ʌ
```

Let's look at multiple `from` clauses which will be converted into `SelectMany` call:

```C#
//----------------------------------V
var chars = new[] { 'a', 'b', 'c' };
var ints = new[] { 2, 3 };
var t0 = from c in chars
         from i in ints
         select (c, i);

IEnumerable<Anon> t1 = chars.SelectMany(c => ints, (c, i) => (c, i));  // this is the method call that the compiler will translate the above query expression into

IEnumerable<Anon> t2 = chars.SelectMany(c => ints.Select(i => (c, i)));  //  performance deteriorated, another nested Select

IEnumerable<Anon> t3 = chars.Bind(c => ints.Map(i => (c, i)));  // SelectMany can be implemented by using Bind and Map

// t1 == t2 == t3

//----------------------------------Ʌ

// a simplified Enumerable.SelectMany() implementation
public static IEnumerable<RR> SelectMany<T, R, RR>(this IEnumerable<T> source, Func<T, IEnumerable<R>> bind, Func<T, R, RR> project)
{
   foreach (T t in source)
      foreach (R r in bind(t))
        yield return project(t, r);
}
```

You can apply the same functional Linq on `Option<T>` as

```C#
//--------------------------------------------V
string s1 = "2", s2 = "3";

Option<int> t4 =         
   from a in Int.Parse(s1)  // you might wonder why below method is called SelectMany when there is no "many thing" like IEnumerable<T>,  <---------this line is important!!!
   from b in Int.Parse(s2)  // it is because when there is multiple "from" clause, it will get translated into SelectMany call,
   select a + b;            // so we can get LINQ's query expression support, now you get the idea :)

// above the method invocation that the LINQ query will be converted to
Option<int> t4 = Int.Parse(s1).SelectMany(a => Int.Parse(s2), (a, b) => a + b);  // Int.Parse(s2) has no connection to a (s1), 
                                                                                 // check the Option<RR> SelectMany source code below you'll get the idea       
// normal method invocation, tedious and not very readable
Option<int> t5 = Int.Parse(s1).Bind(a => Int.Parse(s2).Map(b => a + b));  // check source code to see how a => Int.Parse(s2) works as a and s2 has no connection

// using Apply
Option<int> t6 = Some(new Func<int, int, int>((a, b) => a + b)).Apply(Int.Parse(s1)).Apply(Int.Parse(s2));
//--------------------------------------------Ʌ

                                                                                             // T, R in `project` determine the type between from x in xxx <--------important!
public static Option<RR> SelectMany<T, R, RR>(this Option<T> opt, Func<T, Option<R>> bind, Func<T, R, RR> project)  // T in `bind` doesn't need to be used to converted to Option<R>
{                                                                                                                   // as the example above shows, it is just no connection
   return opt.Match(                                                                                                // for Linq's query syntax which is used purposely,
            () => None,                                                                                             // for most of case, T has connection to Option<R>
            (t) => bind(t).Match(  // t is `a`, and it is not being used for the example above
               () => None,
               (r) => Some(project(t, r))));   // project is a + b
}                         

// standard's IEnumerable's SelectMany comapred to the one above
public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector);

// you can see that R can be IEnumerable or Option or any other types
public static R<TResult> SelectMany<TSource, TCollection, TResult>(this R<TSource> source, Func<TSource, R<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector);

// also note that we don't need to call the genetic type like `TCollection` since it only make senses for IEnumerable<T>, let's call it T2
public static R<TResult> SelectMany<T1, T2, TResult>(this R<T1> source, Func<T1, R<T2>> func, Func<T1, T2, TResult> project);

// now you see it's not necessary for the type to be IEnumerable<T> then compiler can convert into SelectMany call, in fact, Option<T> doesn't implement IEnumerable<T> althrough it could
```

One very important thing to keep in mind is, why `Option<T>`'s extension method `SelectMany` is called "SelectMany" when there is no "Many" to "Select", it makes sense to call `IEnumerable<T>.SelectMany()` because there is "a lot of" thing to select, for example, `IEnumerable<Pet> _ = IEnumerable<Person> _.SelectMany(p => p.Pets)`. But for `Option<T>`, it is just a binary thing internally, why we still call it `SelectMany` on `Option<T>`?  The reason is, we want to be able to use Linq's query expression so when there is multiple `from` clause then it can be converted into `SelectMany`.

And we can also see, the `SelectMany` is actually `Bind` as:

```C#
public static IEnumerable<R> SelectMany<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> f)
{
   foreach (T t in ts)
   {
      foreach (R r in f(t))
      {
         yield return r;
      }
   }
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
```

Another Linq example:

```C#
//---------------------------V
string s1 = "2", s2 = "3";

Option<double> result =
   from a in Double.Parse(s1)
   where a >= 0
   let aa = a * a

   from b in Double.Parse(s2)
   where b >= 0
   let bb = b * b
   select Math.Sqrt(aa + bb);
//---------------------------Ʌ

public static Option<T> Where<T>(this Option<T> optT, Func<T, bool> predicate)
{
   return optT.Match(() => None, (t) => predicate(t) ? optT : None);
}
```


## Monadic Flow (`SelectMany` via Linq or `Bind`) vs Applicative Flow (`Apply`)

```C#
private static ISet<string> ValidCountryCodes = new HashSet<string> { "au", "ch" };

//--------------------------------------------------------------------------------------------V
public record PhoneNumber
{
   public NumberType Type { get; }
   public CountryCode Country { get; }
   public Number Nr { get; }

   public static Func<NumberType, CountryCode, Number, PhoneNumber> Create
      = (type, country, number) => new PhoneNumber(type, country, number);

   private PhoneNumber(NumberType type, CountryCode country, Number number)
   {
      Type = type;
      Country = country;
      Nr = number;
   }

   public override string ToString() => $"{Type}: ({Country}) {Nr}";
}

public enum NumberType { Mobile, Home, Office }

public struct Number
{
   public static Func<string, Option<Number>> Create
      => s => Long.Parse(s)
                  .Map(_ => s)
                  .Where(_ => 5 < s.Length && s.Length < 11)
                  .Map(_ => new Number(s));

   string Value { get; }
   private Number(string value) { Value = value; }
   public static implicit operator string(Number c) => c.Value;
   public static implicit operator Number(string s) => new Number(s);
   public override string ToString() => Value;
}

public class CountryCode
{
   public static Func<ISet<string>, string, Option<CountryCode>> Create
      = (validCodes, code) => validCodes.Contains(code) ? Some(new CountryCode(code)) : None;

   string Value { get; }
   // private ctor so that no invalid instances may be created
   private CountryCode(string value) { Value = value; }
   public override string ToString() => Value;
}
//--------------------------------------------------------------------------------------------Ʌ
```

```C#
//---------------------------------------------------------------------------V
public static Validation<PhoneNumber> CreateValidPhoneNumber_ReturnValidation(string type, string countryCode, string number)  // applicative flow: harvest errors using Apply
{                                                                                                                              
   return Valid(PhoneNumber.Create)
          .Apply(validNumberType(type))
          .Apply(validCountryCode(countryCode))
          .Apply(validNumber(number));
}

//
public static Func<string, Validation<NumberType>> validNumberType
  = str => LaYumba.Functional.Enum.Parse<NumberType>(str).Match(
     None: () => Error($"{str} is not a valid number type"),
     Some: n => Valid(n));

public static Func<string, Validation<CountryCode>> validCountryCode
  = s => CountryCode.Create(ValidCountryCodes, s).Match(
     None: () => Error($"{s} is not a valid country code"),
     Some: c => Valid(c));

public static Func<string, Validation<Number>> validNumber
   = str => Number.Create(str).Match(
      None: () => Error($"{str} is not a valid number"),
      Some: n => Valid(n));
//
//---------------------------------------------------------------------------Ʌ

//------------------------------------------------------------------------------V
public static Validation<PhoneNumber> CreatePhoneNumber_ReturnValidation_UseLinq(string typeStr, string countryStr, string numberStr)  // monadic flow : fail-fast errors
{                                                                                                                                      // use SelectMany internally
   return from type in validNumberType(typeStr)
          from country in validCountryCode(countryStr)
          from number in validNumber(numberStr)
          select PhoneNumber.Create(type, country, number);
}
//------------------------------------------------------------------------------Ʌ

//-------------------------------V
public static Option<PhoneNumber> CreatePhoneNumber_ReturnOption_UseApply(string typeStr, string countryStr, string numberStr)
{
   return Some(PhoneNumber.Create)
          .Apply(optNumberType(typeStr))
          .Apply(optCountryCode(countryStr))
          .Apply(Number.Create(numberStr));
}

//
public static Func<string, Option<CountryCode>> optCountryCode
   = CountryCode.Create.Apply(ValidCountryCodes);

public static Func<string, Option<NumberType>> optNumberType
   = LaYumba.Functional.Enum.Parse<NumberType>;
//
//-------------------------------Ʌ

public static Option<PhoneNumber> CreatePhoneNumber_ReturnOption_UseBind(string typeStr, string countryStr, string numberStr)
{
   return optCountryCode(countryStr)
          .Bind(country => optNumberType(typeStr)   // country -> Option<PhoneNumber>
             .Bind(type => Number.Create(numberStr)
                .Bind<Number, PhoneNumber>(number => PhoneNumber.Create(type, country, number))));
}

public static Option<PhoneNumber> CreatePhoneNumber_ReturnOption_UseLinq(string typeStr, string countryStr, string numberStr)
{
   return from country in optCountryCode(countryStr)
          from type in optNumberType(typeStr)
          from number in Number.Create(numberStr)
          select PhoneNumber.Create(type, country, number);
}

//------------------------------------------------------------------------------------------>>
public static Validation<R> Apply<T, R>(this Validation<Func<T, R>> valF, Validation<T> valT)  // harvesting errors
{                                                                                              
   return valF.Match(
      Valid: (f) => valT.Match
      (
         Valid: (t) => Valid(f(t)),
         Invalid: (err) => Invalid(err)
      ),
      Invalid: (errF) => valT.Match(    // because of the second argument valT, you can still call valT.Match when valF is vinalid, that's why it can harvest errors
         Valid: (_) => Invalid(errF),   // wrap errors when "Valid", quite specail, isn't it :)
         Invalid: (errT) => Invalid(errF.Concat(errT))  // <----------harvesting errors
      )
   );
}

public static Validation<Func<T2, R>> Apply<T1, T2, R>(this Validation<Func<T1, T2, R>> @this, Validation<T1> arg)
{
   return Apply(@this.Map(F.Curry), arg);
}

public static Validation<RR> SelectMany<T, R, RR>(this Validation<T> @this, Func<T, Validation<R>> bind, Func<T, R, RR> project)  // fail-fast errors
{                                                                        // unlike `Apply`'s signature, it uses Func<T, Validation<R>>, that's why it fails-fast errors
   return @this.Match(                                                   
            Invalid: (err) => Invalid(err),    // you can't provide T to bind when @this is invalid, while you can use `Validation<T>` directly in Apply, that's why it fails-fast
            Valid: (t) => bind(t).Match(
               Invalid: (err) => Invalid(err),
               Valid: (r) => Valid(project(t, r))));
}
//------------------------------------------------------------------------------------------<<
```

**Now it is clear to see, `Apply` is about lifting a Func, that's why it is extension methods on `Option<Func<XXX>>` and `Validation<Func<...>>` rather than `Option<T>` or `Validation<T>`**. Let's put the code together again and compare them:

```C#
// applicative flow-----------------------------------------------------V
Some(PhoneNumber.Create)  // function wrapped in Some in the first line
   .Apply(optNumberType(typeStr))
   .Apply(optCountryCode(countryStr))
   .Apply(Number.Create(numberStr));
//----------------------------------------------------------------------Ʌ

// monadic flow---------------------------------------------------------------------------------------------------V
from type in validNumberType(typeStr)
from country in validCountryCode(countryStr)
from number in validNumber(numberStr)
select PhoneNumber.Create(type, country, number);  // function appears in the last line in Select clause, which is the `project` function in the `SelectMany`

public static Validation<RR> SelectMany<T, R, RR>(this Validation<T> @this, Func<T, Validation<R>> bind, Func<T, R, RR> project)  // not exactly the same becauseof 3 froms are used
{                                                                                                                    // project is PhoneNumber.Create function
   return @this.Match(                                                  
            Invalid: (err) => Invalid(err),
            Valid: (t) => bind(t).Match(
               Invalid: (err) => Invalid(err),
               Valid: (r) => Valid(project(t, r))));
}
//-----------------------------------------------------------------------------------------------------------------Ʌ
```

Also, both `Apply` (lifting fuctions first with `Some` then call `Apply` multiple times) and Linq (`SelectMany`) will get you a result of a `Option<T>` or `Validation<T>`, for the former , there is no difference, but for the latter, `Apply` flow harvest errors, while `SelectMany` fails fast errors 

```C#
[TestCase("Mobile", "au", "0400000000", ExpectedResult = "Valid(Mobile: (au) 0400000000)")]
[TestCase("Mobile", "ch", "13800000000", ExpectedResult = "Valid(Mobile: (ch) 13800000000)")]
[TestCase("Office", "us", "911", ExpectedResult = "Invalid([us is not a valid country code, 911 is not a valid number])")]  // two Errors
[TestCase("rubbish", "xx", "1", ExpectedResult = "Invalid([rubbish is not a valid number type, xx is not a valid country code, 1 is not a valid number])")]  // three Errors
public static string ValidPhoneNumberTest(string type, string country, string number)  // test harvesting errors
{
   Validation<PhoneNumber> result = CreateValidPhoneNumber_ReturnValidation(type, country, number);

   return result.ToString();
}

[TestCase("Mobile", "au", "0400000000", ExpectedResult = "Valid(Mobile: (au) 0400000000)")]
[TestCase("Mobile", "ch", "13800000000", ExpectedResult = "Valid(Mobile: (ch) 13800000000)")]
[TestCase("Office", "us", "911", ExpectedResult = "Invalid([us is not a valid country code])")]     // only contains one Error
[TestCase("rubbish", "xx", "1", ExpectedResult = "Invalid([rubbish is not a valid number type])")]  // only contains one Error
public static string ValidPhoneNumberTest2(string type, string country, string number)  // test fail-fast error
{
   Validation<PhoneNumber> result = CreatePhoneNumber_ReturnValidation_UseLinq(type, country, number);

   return result.ToString();
}
```
