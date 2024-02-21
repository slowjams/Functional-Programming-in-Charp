Chapter 1-Introduction to Functional Programming and C# New Features
==============================

## `init` Keyword

Many years C# developers have used `private set` as a way to half achieve immutability:

```C#
public class Person 
{
   public Person()
   {
      Name = "NA";
   }

   public void SetName(string name) 
   {
      this.Name = name;
   }

   public string Name { get; private set; }  // <----------------using private set
}
```

There are two problems here. 

Firstly, you can't use object initializer syntax any more:

```C#
var person = new Person 
{
    Name = "John Citizen"  // <----------------compile error 
};
```

Second Problem, nothing stops developers adding a method (e.g SetName method) inside the class that set the Name Property.

At most of time, what we want is, only assign a value to a propery when the object is created, after that, this property becomes immutable

That's how `init` Properties comes into rescue:

```C#
public class Person 
{
   public string Name { get; init; } = default!;   // note that `default!` forcely assign null to non-nullable because types are non-nullable in C# 8, you can also use `null!` here
                                                   // also note that `= default!` doesn't use up once-in-life-time assignment,it's just placeholder to disable the compiler warnings
   /*
   public void SetName(string name)
   {
      Name = name;  // <----------------compile error
   }
   */
}

var person = new Person { Name = "John" };  // works just fine!

var person2 = new Person();

person.Name = "John"; // Compile Time Error
```

## Record Type

```C#
public record Person   // non-positional syntax
{
   public string FirstName { get; set; } = default!;
   public string LastName { get; set; } = default!;
};

// reference type, implicitly mean class 
//public rec൦rd Person(string FirstName, string LastName);  // positional syntax

// reference type, explicitly
//public rec൦rd class Person(string FirstName, string LastName); 

// struct
//public record struct DailyTemperature(double HighTemp, double LowTemp);
```

To enforce value semantics, the compiler generates several methods for your record type (both for record class types and record struct types):

* An override of `Object.Equals(Object)`
* A virtual Equals method whose parameter is the record type
* An override of `Object.GetHashCode()`
* Methods for operator == and operator !=
* Record types implement `System.IEquatable<T>`

Above is for all recrdo type regardless of non-positional or positional. 

If you declare **positional** records you will get below benefits from the compiler that synthesizes more methods for you when you declare positional records:

* A primary constructor whose parameters match the positional parameters on the record declaration
* Public properties for each parameter of a primary constructor. These properties are init-only for record class and readonly record struct. For record struct, they're read-write
* A `Deconstruct` method to extract properties from the record, even there is only one positional parameter the `Deconstruct` method is still generated (verified by checking the low-level code, so it is NOT you need to have at least two positional parameters to be able to have compiler genertate `Deconstruct` method), but you can't use it unless it is in pattern matching, check this post https://stackoverflow.com/questions/74757953/why-positional-pattern-doesnt-work-for-records-that-have-only-one-positional-pa/74758173#74758173


for example, sometimes we want our class to be immutable, it is cumbersome to setup a lot of properties with `init` keyword, so `record` type comes into rescue:

```C#
public rec൦rd Person(string FirstName, string LastName);   // parameter name is uppercase because it will be the property name as well

Person person1 = new("Nancy", "Davolio");

person1.FirstName = "John"  // compile error

Person person2 = person1 with { FirstName = "John" };  // OK
```

behind the scene, the compiler creates:

```C#
public class Person 
{
   public Person(string firstName, string lastName)  // parameter names doesn't matter
   {   
      FirstName = firstName;
      LastName = lastName;
   }
   
   public string FirstName { get; init; } = default!;
   public string LastName { get; init; } = default!;

   public void Deconstruct(out string fname, out string lname)  // so that you can do: `var (firstName, lastName) = new Person("John", "Citizen")`
   {                                                            // which is the same as: ` (var firstName, var lastName) = new Person("John", "Citizen")`
      fname = FirstName;                                         
      lname = LastName;
   }

   // include those methods compiler create for non-positional records
};
```

if you feel that the generated property pollutes the class's public API,  you can explicitly specify that the property should be private, for example, you don't want FirstName property to be public:

```C#
public rec൦rd Person(string FirstName, string LastName) 
{
   private string FirstName { get; } = FirstName;  // it is like you tell the compiler to overwrite the auto-generate property
}
```

You see how `record` type can save you a lot of keystroke, especailly Person class have 5+ more immutable properties.

Note that `record` type is just to assist you to let you write immutable type easiler, `record` type doesn't mean immutable by force, you can of course design a mutable record type as:

```C#
public record Person 
{
   public string FirstName { get; set; } = default!;
   public string LastName { get; set; } = default!;
};
```

Another benefit to use record type is to let you shallow copy easier

```C#
public rec൦rd Person(string FirstName, string LastName);

Person husband = new("John", "Jackson");

Person wife = husband with { FirstName = "Jenny" };
```

Note that records type are just syntax sugar like `=>` operator, `async/await`,  the compiler generates a constructor, property getters and init setters, and several convenience methods such as `Equals`, `GetHashCode`, and `ToString` for you.

Like normal classes, records type also support inheritance

```C#
public rec൦rd Address(string Country);

public rec൦rd UsAddress(string State) : Address("US");

public static decimal CaculateTaxRate(Address address)
{
   return address switch 
   {
      UsAddress(var state) => XXX(state),
      ("AU") _ => ...,
      (var country) _ => XXX(country)   // check example 3c to see why discard is needed
   };
}
```

Note that you don't really need `init` to make a property can only be initialized during object initializing, you can do as below:

```C#
public rec൦rd Person(string Name)
{
   public string Name { get; } = Name;
}

Person p = new Person("John Citizen");

p.Name = "Michael Smith";  // compiler error just like if you have `init` in the Name property as public string Name { get; init } = Name;
```

but if you go with this approach, you cannot do the shallow copy as:

```C#
Person p1 = new Person("John Citizen");

Person p2 = p1 with { Name = "Michael Smith" };   // onlt `init` enable this feature
```

so it is really against record' purpose and if the `Person` class have a lot of parameters/properties, you have to new up an new instance by providing all the parameter arguments then change the property you want to change, which is quite tedious.

## switch expression, property pattern, `var` pattern

```C#
//-----------------------------------------V switch expression
static string SwitchExpression(Shape shape) 
{
   string whatShape = shape switch {
      Circle r     => $"shape is a circle of radius {r}",
      Rectangle _  =>  "shape is a rectangle",
      _            =>  "shape is null or not a known type of shape"
   };

   return whatShape;
}
//-----------------------------------------Ʌ

//--------------------------------------V property pattern
static void PropertyPattern(Shape shape) 
{
   if(shape is Circle { Radius: 1.0d })
      Console.WriteLine($"shape is a circle of radius 1"); 
 
   string whatShape = shape switch {
      { Name: "unregular" }                     =>  "shape is unregular",   // Shape contains a Name property
      Circle { Radius: 1.0d }                   =>  "shape is a circle of radius 1",
      Rectangle r when r.Width == r.Height      =>  "shape is a square",
      Rectangle { Width: 10d, Height: 5d}       =>  "shape is a 10 x 5 rectangle",
      Rectangle { Width: var x, Height: var y } => $"shape is a {x} x {y} rectangle",
      { }                                       =>  "shape is not null",
      _                                         =>  "null"  // not because `_` matches null, but it is after all other options run out
   };
}

// if rectangle has a deconstructor, the expression Rectangle { Width: var x, Height: var y } can be simplified to  Rectangle(var x, var y) as

public class Rectangle : Shape 
{
   public double Height { get; set; }
   public double Width { get; set; }
   public override double Area => Height * Width;
   public void Deconstruct(out double width, out double height) {
      width = Width; 
      height = Height;
   }
}

public static string WhatShapeWithPositionalPattern(this Shape shape) => shape switch {
   Circle r                 => $"This is a circle of radius {r}",
   Rectangle(var x, var y)  => $"shape is a {x} x {y} rectangle",
   _                        =>  "shape is null or not a known type of shape"
};
//--------------------------------------Ʌ


//--------------------------------------V tuple pattern
public static string ColorsOfWithTuplePattern(Color c1, Color c2, Color c3) =>
   (c1, c2, c3) switch {
      (Color.Blue, Color.White, Color.Red)    => "France",
      (Color.Green, Color.White, Color.Red)   => "Italy",
      (Color.Black, Color.Red, Color.Yellow)  => "Germany",
      _ => throw new ArgumentException("Unknown flag")
   };
//--------------------------------------Ʌ


//----------------------------V positional pattern,  using `var` when the type has Deconstruct
public class Rectangle : Shape {
   public double Height { get; set; }
   public double Width { get; set; }
   public override double Area => Height * Width;
   public void Deconstruct(out double width, out double height) {
      width = Width; 
      height = Height;
   }
}
public static string WhatShapeWithPositionalPattern(this Shape shape) => shape switch {
   Circle r                 => $"This is a circle of radius {r}",
   Rectangle(var x, var y)  => $"shape is a {x} x {y} rectangle",   // <---------------using var that internally calls Deconstruct
   _                        =>  "shape is null or not a known type of shape"
};
//----------------------------Ʌ
```

note that you use a `var` pattern to match any expression, including null, and assign its result to a new local variable:

```C#
//--------------------V example 1
static bool IsAcceptable(int id, int absLimit) =>
    SimulateDataFetch(id) is var results 
    && results.Min() >= -absLimit 
    && results.Max() <= absLimit;

static int[] SimulateDataFetch(int id)
{
    var rand = new Random();
    return Enumerable
               .Range(start: 0, count: 5)
               .Select(s => rand.Next(minValue: -10, maxValue: 11))
               .ToArray();
}
//--------------------Ʌ

//--------------------V example 2
static stringt Vat(Address address)
{
   return address switch
   {
       { Country: "AU" } => "This is a domestic address",
       { Country: var c } => $"This is an international address, country is {c}",
   }
}
//--------------------Ʌ

//--------------------V exmaple 3a
int? maybe = null;

if (maybe is int number)
{
    Console.WriteLine($"The nullable int 'maybe' has the value {number}");
}
else
{
    Console.WriteLine("The nullable int 'maybe' doesn't hold a value");   // <-------------go to this path
}
//--------------------Ʌ

//--------------------V exmaple 3b 
int? maybe = null;

if (maybe is var number)
{
    // ...            // <----------always go to this path, because var match null; this has an effect on how to distinct Deconstruct and var pattern in parentheses as example 3c
}
else
{
    // ...            // code won't go to this path
}
//--------------------Ʌ

//--------------------V exmaple 3c
public rec൦rd Person(string Name);

Person p = new("John Citizen");

if (p is var name)
{
    // name is of type Person
}

if (p is (var name))
{
    // name is of type Person
}

if (p is (var name) pp)
{
    // name is of type string
    // pp is of type Person
}

if (p is (var name) _)
{
    // name is of type string
    // _ is of type Person
}
```

```C#
// another example
public struct Rectangle
{
  public double Length { get; init; }
  public double Height {get; init; }

  public void Deconstruct(out double length, out double height)
  {
    length = Length;
    height = Height;
  }
}

Rectangle rectangle = new Rectangle { Length = 20, Height = 40 };
var (l, h) = rectangle;

if (rectangle is (20, _) rect)  // rect is Rectangle, which you can use a discard `_` instead
{
   Console.WriteLine("The rectangle has a length of 20");
}

// or you can write
if (rectangle is (20, _) _)  // rect can be discard
{
   Console.WriteLine("The rectangle has a length of 20");
}

// you might ask why we need to use `rect` or discard `_` as a new variable when `rectangle` can still be used, the reason is if you define the Rectangle type as

public struct Rectangle
{
  public string Name { get; init; }

  public void Deconstruct(out string name)
  {
     name = Name;
  }
}

// ...

if (rectangle is (var name))  // if we get rid of _ discard or any new variable name here
{
   Console.WriteLine("The rectangle has a length of 20");
} 

// above will be the same as

if (rectangle is var name)  // name is Rectangle, not string 
{
   // code always execute since `var` match any type
} 

// in order to distinct Deconstruct and var pattern in parentheses as example, you need to add `_ `, now you see why we need to do (var country) _ => XXX(country)
//--------------------Ʌ

```


## Switch Feature

```C#
public class Bank 
{
   public BankBranchStatus Status {get;set;}
}

public enum BankBranchStatus 
{
   Open,
   Closed,
   VIPCustomersOnly
}

// old approach
static bool CheckIfCanWalkIntoBankSwitch(Bank bank, bool isVip) 
{
   bool result = false;
   switch(bank.Status) {
      case BankBranchStatus.Open : 
         result = true;
         break;
        
      case BankBranchStatus.Closed : 
         result = false;
         break;
        
      case BankBranchStatus.VIPCustomersOnly : 
         result = isVip;
         break;
   }
   return result;
}
```
in above example, result is return anyway, but what if you want the result and further process it in the method? you can't do `result = switch(bank.Status)`  to set the result. Let's what C# 8 comes into rescue:

```C#
// modern C# 8 approach
static bool CheckIfCanWalkIntoBankSwitch(Bank bank, bool isVip) 
{
   var result = bank.Status switch 
   {
      BankBranchStatus.Open => true,
      BankBranchStatus.Closed => false,
      BankBranchStatus.VIPCustomersOnly => isVip
   };
   // ... further process result if you want
   return result;
}

static bool CheckIfCanWalkIntoBank(Bank bank, bool isVip) 
{
   return bank.Status switch 
   {
      BankBranchStatus.Open => true, 
      BankBranchStatus.Closed => false, 
      BankBranchStatus.VIPCustomersOnly when isVip => true, 
      BankBranchStatus.VIPCustomersOnly when !isVip => false
   };
}

// combined with tuple
static bool CheckIfCanWalkIntoBank(Bank bank, bool isVip)    // `_` underscore signify that it should match anything
{   
   return (bank.Status, isVip) switch 
   {
      (BankBranchStatus.Open, _) => true,                    // we use underscore because if the bank is closed then it's closed, does't matter if clients are a VIP
      (BankBranchStatus.Closed, _) => false, 
      (BankBranchStatus.VIPCustomersOnly, true) => true, 
      (BankBranchStatus.VIPCustomersOnly, false) => false
   };
}

// pass objects to a switch statement and inspect their properties, looks like javascript
static bool CheckIfCanWalkIntoBank(Bank bank, bool isVip) 
{
   return bank switch 
   {
      { Status : BankBranchStatus.Open}  => true, 
      { Status : BankBranchStatus.Closed } => false, 
      { Status : BankBranchStatus.VIPCustomersOnly }  => isVip
   };
}

// `{}` and `()` are sometimes interchangeable, you can use `()` if underlying types are record types with two or more posistional fields or classes who have Deconstruct methods

public class Order 
{
   public int Items { get; init; } = default!;
   public decimal Cost { get; init; } = default!;

   public void Deconstruct(out string items, out string cost) {
        items = Items;
        cost = Cost;
   }
}

// or if Order is record type
// public record Order(int Items, decimal Cost);

public decimal CalculateDiscount(Order order) {
   order switch {
      (Items: > 10, Cost: > 1000.00m) => 0.10m,                // when use pattern match, variable name has to match propery name, so you can't do (items: > 10, cost: > 1000.00m)
      (Items: > 5, Cost: > 500.00m) => 0.05m,                  // same as above
      var (item, cost) when item > 100 => item * cost * 0.5,   // when use deconstruct method, variable name doesn't need to match propery name
      Order { Cost: > 250.00m } => 0.02m,                      // hard to describe, but you get the idea :)
      null => throw new ArgumentNullException(nameof(order), "Can't calculate discount on null order"),
      _ => 0m
   };
}

// you can also omit the property names
public decimal CalculateDiscount(Order order) {
   order switch {
      (> 10, > 1000.00m) => 0.10m,
      (> 5, > 500.00m) => 0.05m,
      // ...
      var someObject => 0m  // is equivalent to `_ => 0m`
   };
}
```