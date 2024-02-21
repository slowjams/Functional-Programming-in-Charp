Chapter 17-Traversable
=========================================

```C#
public static void Main(string[] args)
{
    string input = Console.ReadLine();

    /*
    let's say you want to parse the string input such as "1,2,3" with ',' as delimiter, then the result should be 6,
    and there could be some substring that cannot be parsed such as "1, two, 3"?

    Also, the traditional approach will get you IEnumerable<Option<double>>, but what we really want is Option<IEnumerable<double>>,
    that's when "Traverse" comes into play
    */

    //----------------------------------------------------------------------------------------V  not so good traditional approach
    IEnumerable<Option<double>> numsOpt = input.Split(',').Map(String.Trim).Map(Double.Parse);  // IEnumerable<Option<double>> is not what we want
    IEnumerable<double> num = numsOpt.SelectMany(opt => opt.AsEnumerable(), (opt, d) => d);     // need extra work to convert IEnumerable<Option<double>> into IEnumerable<double>
    double result = num.Sum();                                                                  
    //----------------------------------------------------------------------------------------Ʌ

    //------------------------------------------------------------------------------------------------------------V
    Process_Option("1, 2, 3");       // => "The sum is 6"
    Process_Option("one, two, 3");   // => "Some of your inputs could not be parsed"

    Process_Validation_FailFast("1, 2, 3");       // => "The sum is 6"
    Process_Validation_FailFast("one, two, 3");   // => "'one' is not a valid number"

    Process_Validation_Harvest("1, 2, 3");       // => "The sum is 6"
    Process_Validation_Harvest("one, two, 3");   // => "'one' is not a valid number, 'two' is not a valid number"
    //------------------------------------------------------------------------------------------------------------Ʌ

    //------------------------------------------------------------------------------------------------------------V
    Validator<string> ShouldBeLowerCase =
        s => (s == s.ToLower()) ? Valid(s) : Error($"{s} should be lower case");

    // this is a local function
    Validator<string> ShouldBeOfLength(int n) => s => (s.Length == n) ? Valid(s) : Error($"{s} should be of length {n}");

    Validator<string> ValidateCountryCode_Apply_All_Validations = HarvestErrors(ShouldBeLowerCase, ShouldBeOfLength(2));

    ValidateCountryCode_Apply_All_Validations("us");   // => Valid(us)

    ValidateCountryCode_Apply_All_Validations("US");   // => Invalid([US should be lower case])

    ValidateCountryCode_Apply_All_Validations("USA");  // => Invalid([USA should be lower case, USA should be of length 2])
    //------------------------------------------------------------------------------------------------------------Ʌ
}

public static string Process_Option(string input)  // <------------better approach, but it won't show users the error details
{
    return
        input.Split(',')         // Array<string>
        .Map(String.Trim)        // IEnumerable<string>
        .Traverse(Double.Parse)  // Option<IEnumerable<double>>
        .Map(Enumerable.Sum)     // Option<double>
        .Match
        (
            () => "Some of your inputs could not be parsed",
            (sum) => $"The sum is {sum}"
        );
}

//------------------------------------------------->>
public static string Process_Validation_FailFast(string input)
{
    return
        input.Split(',')
        .Map(String.Trim)
        .TraverseM(Validate)
        .Map(Enumerable.Sum)
        .Match
        (
            errs => string.Join(", ", errs),  // even though it fails fast, it can still be multiple errors when it fails the first time
            (sum) => $"The sum is {sum}"
        );
}

public static string Process_Validation_Harvest(string input)
{
    return
        input.Split(',')         // Array<string>
        .Map(String.Trim)        // IEnumerable<string>
        .TraverseA(Validate)     // Validation<IEnumerable<double>>
        .Map(Enumerable.Sum)     // Validation<double>
        .Match
        (
            errs => string.Join(", ", errs),
            (sum) => $"The sum is {sum}"
        );
}
//-------------------------------------------------<<

public static Validation<double> Validate(string s)
    => Double.Parse(s).Match
    (
        () => Error($"'{s}' is not a valid number"),
        (d) => Valid(d)
    );


public static Option<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> ts, Func<T, Option<R>> f)
{
    return ts.Aggregate
    (
        seed: Some(Enumerable.Empty<R>()),
        func: (optRs, t) =>
           from rs in optRs
           from r in f(t)
           select rs.Append(r)
    );
}

//--------------------------------------------------------------------------------------------------------V fail-fast errors
public static Validation<IEnumerable<R>> TraverseM<T, R>(this IEnumerable<T> ts, Func<T, Validation<R>> f)
{
    return ts.Aggregate
    (
        seed: Valid(Enumerable.Empty<R>()),
        func: (valRs, t) =>
           from rs in valRs
           from r in f(t)
           select rs.Append(r)
    );
}

public static Validation<RR> SelectMany<T, R, RR>(this Validation<T> @this, Func<T, Validation<R>> bind, Func<T, R, RR> project)
{
    return @this.Match
    (
        Invalid: (err) => Invalid(err),
        Valid: (t) => bind(t).Match(
            Invalid: (err) => Invalid(err),
            Valid: (r) => Valid(project(t, r)))
    );
}
//--------------------------------------------------------------------------------------------------------Ʌ

//---------------------------------------------------------------V harvest errors
public static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()
{
    return (ts, t) => ts.Append(t);
}

public static Validation<IEnumerable<R>> TraverseA<T, R>(this IEnumerable<T> ts, Func<T, Validation<R>> f)
{
    return ts.Aggregate
    (
        seed: Valid(Enumerable.Empty<R>()),
        func: (valRs, t) =>
           Valid(Append<R>())
           .Apply(valRs)
           .Apply(f(t))
    );
}
//---------------------------------------------------------------Ʌ

//---------------------------------------------------------------------------V
public static Validator<T> HarvestErrors<T>(params Validator<T>[] validators)   // compress multiple validators into a single Validator
{
    return t => validators
                .TraverseA_(validate => validate(t)) // Validation<IEnumerable<MakeTransfer>> where IEnumerable<MakeTransfer> points to a single MakeTransfer instance, weird
                .Map(_ => t);   // _ is IEnumerable<MakeTransfer>, which you want to discard
}

public static Validation<IEnumerable<R>> TraverseA_<T, R>(this IEnumerable<T> ts, Func<T, Validation<R>> f)  // f is validate => validate(t)
{   // return Validation<IEnumerable<MakeTransfer>>                                                          
    return ts.Aggregate                                // T is Validator<MakeTransfer>, R is MakeTransfer
    (
        seed: Valid(Enumerable.Empty<R>()),  // Enumerable.Empty<MakeTransfer>()
        func: (valRs, t) =>   // t is Validator<MakeTransfer>, valRs is Validation<IEnumerable<MakeTransfer>>
           Valid(Append_<R>())
           .Apply(valRs)
           .Apply(f(t))   // f(t) is Validation<MakeTransfer>
    );
}  // note that IEnumerable<R> in the return type of Validation<IEnumerable<R>> is not deemed as usable when IEnumerable<T> is Validator<T>[]
   // as IEnumerable<R> emulates the same R instance, e.g same MakeTransfer instance

public static Func<IEnumerable<T>, T, IEnumerable<T>> Append_<T>()  // t is MakeTransfer
{
    return (ts, t) => ts.Append(t);
}
//---------------------------------------------------------------------------Ʌ
    
public delegate Validation<T> Validator<T>(T t);

/*
private static Validation<MakeTransfer> ValidateBic(MakeTransfer transfer)
   => bicRegex.IsMatch(transfer.Bic)
      ? transfer
      : Errors.InvalidBic;

private static Validation<MakeTransfer> ValidateDate(MakeTransfer transfer)
   => transfer.Date.Date > now.Date
      ? transfer
      : Errors.TransferDateIsPast;
*/
```


## Task Traversable

```C#
public static class TasksTraversable
{
    public static Airline jetstar = default;
    public static Airline tiger = default;

    public static void Main()
    {
        //------------------------------------------------------------------------------V bad, use Map
        IEnumerable<Airline> airlines = default!;
        string from = "Australia", to = "China";
        DateTime departure = DateTime.Now.AddDays(15);

        IEnumerable<Task<IEnumerable<Flight>>> flights = airlines.Map(a => a.Flights(from, to, departure));  // IEnumerable<Task<IEnumerable<Flight>>> is not what we want
        //------------------------------------------------------------------------------Ʌ

        //------------------------------------------------------------------------------V good approach
        Task<IEnumerable<IEnumerable<Flight>>> result = airlines.Traverse(a => a.Flights(from, to, departure));
        IEnumerable<IEnumerable<Flight>> result_ = await result;   // can write those this and above line into one statement, just for demo and comparision purpose
        IEnumerable<Flight> resultUnwrap = resultFinal.Flatten().OrderBy(f => f.Price);
        //------------------------------------------------------------------------------Ʌ
    }

    // Task<IEnumerable<IEnumerable<Flight>>>, R is IEnumerable<Flight>, T is Airline
    public static Task<IEnumerable<R>> TraverseA<T, R>(this IEnumerable<T> ts, Func<T, Task<R>> f)  // by default use applicative TraverseA (parallel, hence faster)
    {
        return ts.Aggregate
        (
            seed: Task.FromResult(Enumerable.Empty<R>()),   // itself and rs are Task<IEnumerable<IEnumerable<Flight>>>
            func: (rs, t) => Task.FromResult(Append<R>())   // t is Airline
                                 .Apply(rs)
                                 .Apply(f(t))
        );
    }

    public static Task<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> list, Func<T, Task<R>> func)
    {                                                                             // Airline -> Task<IEnumerable<Flight>>
        return TraverseA(list, func);
    }

    public static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()  // T is IEnumerable<Flight>
    {
        return (ts, t) => ts.Append(t);
    }
}

//------------------------------------------------------------------------>>
public interface Airline
{
    //Task<Flight> BestFare(string from, string to, DateTime on);
    Task<IEnumerable<Flight>> Flights(string from, string to, DateTime departure);
}

public class Flight
{
    public decimal Price { get; set; }
}
//------------------------------------------------------------------------<<
```


## More Traversable Extension Methods

```C#
public static class ValidationTraversable
{
    // Exceptional
    public static Exceptional<Validation<R>> Traverse<T, R>(this Validation<T> @this, Func<T, Exceptional<R>> f)
    {
        return @this.Match
        (
            Invalid: errs => Exceptional(Invalid<R>(errs)),
            Valid: t => f(t).Map(Valid)  // Valid is F.Valid below
        );
    }

    /*
    public static partial class F
    {
        public static Validation<T> Valid<T>(T value) => new(value ?? throw new ArgumentNullException(nameof(value)));
        // ...
    }
    */


    // Task
    public static Task<Validation<R>> Traverse<T, R>(this Validation<T> @this, Func<T, Task<R>> func)
    {
        return @this.Match
        (
           Invalid: reasons => Async(Invalid<R>(reasons)),
           Valid: t => func(t).Map(Valid)   // Valid is F.Valid above
        );
    }

    // more, can be added in the future when needed
}
```
