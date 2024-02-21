using System;
using System.Collections.Generic;
using System.Text;
using Unit = System.ValueTuple;
using Boc.Domain.Events;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;
using ConsoleAppFP.Model;
using Double = LaYumba.Functional.Double;
using String = LaYumba.Functional.String;
using System.Linq;

namespace ConsoleAppFP.Chapterz17_Traversable
{
    public static class Program17_Traversable
    {      
        public static void Main__(string[] args)
        {
            string input = Console.ReadLine();

            //----------------------------------------------------------------------------------------V  not so good approach
            IEnumerable<Option<double>> numsOpt = input.Split(',').Map(String.Trim).Map(Double.Parse);
            IEnumerable<double> num = numsOpt.SelectMany(opt => opt.AsEnumerable(), (opt, d) => d);
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

        public static string Process_Option(string input)  // <----------better approach, but it won't show users the error details
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
                   from r  in f(t)
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
                        .TraverseA_(validate => validate(t))  // Validation<IEnumerable<MakeTransfer>> where IEnumerable<MakeTransfer> only points to a single MakeTransfer input instance, which is weird 
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
    }

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
}