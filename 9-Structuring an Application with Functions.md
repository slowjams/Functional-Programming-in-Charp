Chapter 9-Structuring an Application with Functions
==============================

## Partial Application: Supplying Arguments Piecemeal

```C#
using Name = System.String;
using Greeting = System.String;
using PersonalizedGreeting = System.String;

var greet = (Greeting gr, Name name) => $"{gr}, {name}";

Name[] names = { "Tristan", "Ivan" };

names.Map(n => greet("Hello", n)).ForEach(WriteLine);   // public static IEnumerable<R> Map<T, R>(this IEnumerable<T> list, Func<T, R> func) => list.Select(func);

/*output:
  Hello, Tristan
  Hello, Ivan
*/
```

Notice that the greet function is always called with "Hello" as its first argument, whereas the second argument varies with each name in the list.
Wouldn't it be better to fix the greeting to be "Hello" outside the scope of Map? We can solve this with partial application.


#### Manually Enabling Partial Application

One way to supply arguments independently would be to rewrite the greet function as:

```C#
var greetWith = (Greeting gr) => (Name name) => $"{gr}, {name}";

var greetFormally = greetWith("Good evening");

names.Map(greetFormally).ForEach(WriteLine);

/*output:
  Good evening, Tristan
  Good evening, Ivan
*/
```
`greetWith` is said to be in **curried** form: all arguments are supplied one by one via function invocation.


####  Generalizing Partial Application using `Apply`

We want to use existing function like `greet` and convert it to `greetWith` without rewriting the function:

```C#
public static Func<T2, R> Apply<T1, T2, R>(this Func<T1, T2, R> f, T1 t1)   // `greet` is f in this example, return function is `greetWith`
{
   return t2 => f(t1, t2);
}

public static Func<T2, T3, R> Apply<T1, T2, T3, R>(this Func<T1, T2, T3, R> f, T1 t1)
{
   return (t2, t3) => f(t1, t2, t3);
}

public static Func<T2, T3, T4, R> Apply<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, R> func, T1 t1)
{
   return (t2, t3, t4) => func(t1, t2, t3, t4);
}

// ... mopre overloads

// You can see that using Apply is like you let the complier generate another new anonymous method which calls 
// the original method pointed by f, then you return this the new Func which points to this new anonymous method
```

```C#
// exampleOne
var greetInformally = greet.Apply("Good evening");

names.Map(greetFormally).ForEach(WriteLine);
```

```C#
// exampleTwp
Func<int, int, int> multiply = (int x, int y) => x * y;

var result =  // result is 12
   multiply
   .Apply(3)
   .Apply(4)

```


##### Order of Arguments Matters

The `greet` function shows what is generally a good order of arguments: the more general parameters, which are likely to be applied early in the life of the application, should come first, followed by the more specific parameters. We learn to say Hello early in life, but we keep meeting and greeting new people until we're old. As a rule of thumb, if you think of a function as an operation, its arguments typically include the following:

* The data that the operation will affect. This is likely to be given late and should be left last

* Some options that determine how the function will operate or dependencies that the function requires to do its work. These are likely to be determined early and should come first


##  Curried Functions: Optimized for Partial Application

Named after mathematician Haskell Curry, **currying** is the process of transforming an n-ary function f that takes the arguments t1, t2,..., tn into a unary function that takes t1, and yields a new function that takes t2, and so on, ultimate returning the same result as f once the arguments have all been given:

```C#
var greet = (Greeting gr, Name name) => $"{gr}, {name}";

var greetWith = (Greeting gr) => (Name name) => $"{gr}, {name}";   // `greetWith` is curried form of `greet`

greetWith("hello")("John")  // => "hello, John"
```

A function can be written in curried form like `greetWith` is called *manual currying*. Alternately, we can define generic functions that will take an n-ary function and curry it:

```C#
public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> f) 
{
   return t1 => t2 => f(t1, t2);
}

public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(this Func<T1, T2, T3, R> f)
{
   return t1 => t2 => t3 => f(t1, t2, t3);
}

public static Func<T1, Func<T2, Func<T3, Func<T4, R>>>> Curry<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, R> f)
{
   return t1 => t2 => t3 => t4 => f(t1, t2, t3, t4);
}
// ...
```

we can use such a generic `Curry` function to curry the `greet` fucntion:

```C#
var greetWith = greet.Curry();

greetWith("hello")("John")  // => "hello, John"
```

Partial application and currying are closely related but not the same, the differences are:

* Partial Application: You give a function fewer arguments than the function expects, obtaining a function that's particularized with the values of the arguments given so far

* Currying: You don't give any argument, you just transform an n-ary function into a unary function that its arguments can be given successively

You can do partial application without currying as we did previously in this chapter with the use of the generic `Apply` functions, but currying makes it eaiser for partial application as you can see that for the `Apply` function like:

```C#
public static Func<T2, T3, R> Apply<T1, T2, T3, R>(this Func<T1, T2, T3, R> f, T1 t1)
{
   return (t2, t3) => f(t1, t2, t3);
}
```

You provide the first argument t1 and then it returns a function that you still need to supply t2 and t3 in once, if you want to supply only one argument each time, then you have to call `Apply` multiple times as below:
 
```C#
// Hello, XXX, Goodbye
var twoPhaseLeftGreeting = threePhaseGreeting.Apply("Hello");

string greeting = twoPhaseLeftGreeting("Goodbye", "XXX");  

string greeting = threePhaseGreeting.Apply("Hello").Apply("Goodbye")("XXX");
```

Currying saves you calling multiple `Apply()`:

```C#
var curriedGreeting = threePhaseGreeting.Curry();

string greeting = curriedGreeting("Hello")("Goodbye")("XXX");  // currying let you use partial application more easily
```


## Particularizing the Data Access API

use types to make our connection string more expressive, so it makes much more sense to do:

```C#
// good, more explicit than depending on a string, also you can now define extension methods on ConnectionString, which wouldn't make sense on string
public Option<Employee> lookupEmployee (ConnectionString conn, Guid id);

// bad, less  intention-revealing
public Option<Employee> lookupEmployee (string conn, Guid id);
```

```C#
//------------------------------------------V
public record ConnectionString(string Value)
{
   public static implicit operator string(ConnectionString c) => c.Value;
   public static implicit operator ConnectionString(string s) => new(s);
}

public record SqlTemplate(string Value)
{
   public static implicit operator string(SqlTemplate c) => c.Value;
   public static implicit operator SqlTemplate(string s) => new(s);
}
//------------------------------------------Ʌ

//----------------------------------V
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
//----------------------------------Ʌ

//--------------------------------------------V
public static class ConnectionStringExtensions
{
   public static Func<object, IEnumerable<T>> Retrieve<T>(this ConnectionString connStr, SqlTemplate sql)
   {
      return param => Connect(connStr, conn => conn.Query<T>(sql, param));
   }
}
//--------------------------------------------Ʌ

public class Program
{
   public static void Main(string[] args)
   {
      ConnectionString conn = configuration.GetSection("ConnectionString").Value;

      SqlTemplate sel = "SELECT * FROM EMPLOYEES"
         , sqlById = $"{sel} WHERE ID = @Id"
         , sqlByName = $"{sel} WHERE LASTNAME = @LastName";

      // queryById
      Func<object, IEnumerable<T>> queryById = conn.Retrieve<Employee>(sqlById);   // connection string and SQL query are fixed 
      // queryByLastName 
      Func<object, IEnumerable<T>>  queryByLastName = conn.Retrieve<Employee>(sqlByName);  // connection string and SQL query are fixed

      // local functions
      Option<Employee> lookupEmployee(Guid id) => queryById(new { Id = id }).SingleOrDefault();
      IEnumerable<Employee> findEmployeesByLastName(string lastName) => queryByLastName(new { LastName = lastName });
      //
   }
}
```


## Modularizing and composing an application

Let's first look at the traditional OO approach

```C#
//----------------------------------->>
public interface IValidator<T>
{
   Validation<T> Validate(T request);
}

public interface IRepository<T>
{
   Option<T> Lookup(Guid id);
   Exceptional<Unit> Save(T entity);
}

public class DateNotPastValidator : IValidator<MakeTransfer>
{
   private readonly IDateTimeService clock;

   public DateNotPastValidator(IDateTimeService clock)
   {
      this.clock = clock;
   }

   public Validation<MakeTransfer> Validate(MakeTransfer request)
   {
      if (request.Date.Date <= clock.UtcNow.Date)
         return Errors.TransferDateIsPast;

      return request;
   }
}
//-----------------------------------<<

//---------------------------------V
public class MakeTransferController : ControllerBase
{
   IValidator<MakeTransfer> validator;
   IRepository<MakeTransfer> repository;

   public MakeTransferController(IValidator<MakeTransfer> validator, IRepository<MakeTransfer> repository)
   {
      this.validator = validator;
      this.repository = repository;
   }

   //[HttpPost, Route("api/Chapters7/transfers/future")]
   public IActionResult MakeTransfer([FromBody] MakeTransfer cmd)
      => validator.Validate(cmd)
         .Map(repository.Save)
         .Match(
            Invalid: BadRequest,
            Valid: result => result.Match<IActionResult>(
               Exception: _ => StatusCode(500, Errors.UnexpectedError),
               Success: _ => Ok()));
}
//---------------------------------Ʌ
```


#### FP Approach:

```C#
//----------------------------------------------->>
public delegate Validation<T> Validator<T>(T t);  //<------------------------------important

public static class Validation
{
   public static Validator<MakeTransfer> DateNotPast(Func<DateTime> clock)
   {
      return transfer => transfer.Date.Date < clock().Date ? Errors.TransferDateIsPast : Valid(transfer);
   }
}
//-----------------------------------------------<<

[ApiController]
[Route("[controller]")]
public class MakeTransferController_v4_Modularity : ControllerBase
{
   private DateTime now;
   private Regex bicRegex = new("[A-Z]{11}");
   private ILogger<MakeTransferController_v3_Validation_Exceptional> logger;

   private Validator<MakeTransfer> validate;             // ----------------------injected
   private Func<MakeTransfer, Exceptional<Unit>> save;   // ----------------------injected

   [HttpPost]
   [Route("transfers/book")]
   public IActionResult MakeTransfer([FromBody] MakeTransfer transfer)   //---------------
      => validate(transfer).Map(save).Match<IActionResult>
      (
         Invalid: BadRequest,
         Valid: result => result.Match
         (
            Exception: OnFaulted,
            Success: _ => Ok()
         )
      );

   [HttpPost]
   [Route("transfers/book")]
   public IActionResult MakeTransferFromV3([FromBody] MakeTransfer transfer)
     => Handle(transfer).Match<IActionResult>
     (
        Invalid: BadRequest,
        Valid: result => result.Match
        (
           Exception: OnFaulted,
           Success: _ => Ok()
        )
     );

   private IActionResult OnFaulted(Exception ex)
   {
      logger.LogError(ex.Message);
      return StatusCode(500, Errors.UnexpectedError);
   }

   private Validation<Exceptional<Unit>> Handle(MakeTransfer transfer)
      => Validate(transfer)
         .Map(Save);

   private Validation<MakeTransfer> Validate(MakeTransfer transfer)   // top-level validation function combining various validation rules
      => ValidateBic(transfer)
         .Bind(ValidateDate);
         
    

   private Exceptional<Unit> Save(MakeTransfer cmd)
   {
      try
      {
         // ...
      }
      catch (Exception ex)
      {
         return ex;
      }

      return Unit.Create();
   }
}
```

## Why need controllers? Mapping functions to API endpoints

```C#
public static class Program
{
   public async static Task Run()
   {
      WebApplication app = WebApplication.Create();

      Func<MakeTransfer, IResult> handleSaveTransfer = ConfigureSaveTransferHandler(app.Configuration);

      app.MapPost("/Transfer/Future", handleSaveTransfer);

      await app.RunAsync();
   }

   public static Func<MakeTransfer, IResult> ConfigureSaveTransferHandler(IConfiguration config)
   {
      ConnectionString connString = config.GetSection("ConnectionString").Value;

      SqlTemplate InsertTransferSql = "INSERT ...";

      Func<object, Exceptional<Unit>> save = connString.TryExecute(InsertTransferSql);

      Validator<MakeTransfer> validate = Validation.DateNotPast(() => DateTime.UtcNow);   // "bake" Func<DateTime> () => DateTime.UtcNow into returned delegate

      return HandleSaveTransfer(validate, save);  // "bake" thoese delegates into another returned delegate
   }

   public static Func<MakeTransfer, IResult> HandleSaveTransfer(Validator<MakeTransfer> validate,
                                                               Func<MakeTransfer, Exceptional<Unit>> save)
   {
      return transfer => validate(transfer).Map(save).Match
      (
         err => BadRequest(err),
         result => result.Match(_ => StatusCode(StatusCodes.Status500InternalServerError), _ => Ok())
      );
   }
}
```

## Fail-Fast and Harvesting Validation Errors

```C#
//----------------------------------------------V
public static partial class ValidationStrategies
{
   public static Validator<T> FailFast<T>(IEnumerable<Validator<T>> validators)
   {
      return t => validators.Aggregate(Valid(t), (acc, validator) => acc.Bind(_ => validator(t)));
   }

   public static Validator<T> HarvestErrors<T>(IEnumerable<Validator<T>> validators)
   {
      return t =>
      {
         IEnumerable<IEnumerable<Error>> errors =
            validators.Map(validator => validator(t))  // Map returns IEnumerable<Validation<T>>
                      .Bind(v => v.Match(errors => Some(errors), _ => None));  // .Bind(Func<Validation<T>, Option<IEnumerable<Error>>>)
                      // interestingly, `Some` wraps errors when "invalid", and `None` when "valid" the reason is you want to retrieve errors

         return errors.ToList().Count == 0 ? Valid(t) : Invalid(errors.Flatten());
      };
   }
}

// public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> list, Func<T, Option<R>> func);
//----------------------------------------------Ʌ

//---------------------------------------------->>
public delegate Validation<T> Validator<T>(T t);
//----------------------------------------------<<


public static partial class ValidationStrategiesTest
{
   static readonly Validator<int> Success = i => Valid(i);
   static readonly Validator<int> Failure = _ => Error("Invalid");

   public class FailFastTest
   {
      [Test]
      public void WhenAllValidatorsSucceed_ThenSucceed() => Assert.AreEqual(
         actual: FailFast(List(Success, Success))(1),
         expected: Valid(1)
      );

      [Test]
      public void WhenNoValidators_ThenSucceed() => Assert.AreEqual(
         actual: FailFast(List<Validator<int>>())(1),
         expected: Valid(1)
      );

      [Test]
      public void WhenOneValidatorFails_ThenFail() =>
         FailFast(List(Success, Failure))(1).Match(
            Valid: (_) => Assert.Fail(),
            Invalid: (errs) => Assert.AreEqual(1, errs.Count()));

      [Test]
      public void WhenSeveralValidatorsFail_ThenFail() =>
         FailFast(List(Success, Failure, Failure, Success))(1).Match(
            Valid: (_) => Assert.Fail(),
            Invalid: (errs) => Assert.AreEqual(1, errs.Count())); // only the first error is returned
   }

   public class HarvestErrorsTest
   {
      [Test]
      public void WhenAllValidatorsSucceed_ThenSucceed() => Assert.AreEqual(
         actual: HarvestErrors(List(Success, Success))(1),
         expected: Valid(1)
      );

      [Test]
      public void WhenNoValidators_ThenSucceed() => Assert.AreEqual(
         actual: HarvestErrors(List<Validator<int>>())(1),
         expected: Valid(1)
      );

      [Test]
      public void WhenOneValidatorFails_ThenFail() =>
         HarvestErrors(List(Success, Failure))(1).Match(
            Valid: (_) => Assert.Fail(),
            Invalid: (errs) => Assert.AreEqual(1, errs.Count()));

      [Test]
      public void WhenSeveralValidatorsFail_ThenFail() =>
         HarvestErrors(List(Success, Failure, Failure, Success))(1).Match(
            Valid: (_) => Assert.Fail(),
            Invalid: (errs) => Assert.AreEqual(2, errs.Count())); // all errors are returned
   }
}
```