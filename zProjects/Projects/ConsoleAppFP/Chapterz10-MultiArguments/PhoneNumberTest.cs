using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Double = LaYumba.Functional.Double;

namespace ConsoleAppFP.Chapterz10_MultiArguments
{
   public static partial class Program9_2
   {
      [TestCase("Mobile", "au", "0400000000", ExpectedResult = "Valid(Mobile: (au) 0400000000)")]
      [TestCase("Mobile", "ch", "13800000000", ExpectedResult = "Valid(Mobile: (ch) 13800000000)")]
      [TestCase("Office", "us", "911", ExpectedResult = "Invalid([us is not a valid country code, 911 is not a valid number])")]  // two Errors
      [TestCase("rubbish", "xx", "1", ExpectedResult = "Invalid([rubbish is not a valid number type, xx is not a valid country code, 1 is not a valid number])")]  // three Errors
      public static string ValidPhoneNumberTest(string type, string country, string number)
      {
         Validation<PhoneNumber> result = CreateValidPhoneNumber_ReturnValidation(type, country, number);

         return result.ToString();
      }

      [TestCase("Mobile", "au", "0400000000", ExpectedResult = "Valid(Mobile: (au) 0400000000)")]
      [TestCase("Mobile", "ch", "13800000000", ExpectedResult = "Valid(Mobile: (ch) 13800000000)")]
      [TestCase("Office", "us", "911", ExpectedResult = "Invalid([us is not a valid country code])")]
      [TestCase("rubbish", "xx", "1", ExpectedResult = "Invalid([rubbish is not a valid number type])")]  // only one Error
      public static string ValidPhoneNumberTest2(string type, string country, string number)
      {
         Validation<PhoneNumber> result = CreatePhoneNumber_ReturnValidation_UseLinq(type, country, number);

         return result.ToString();
      }

      public static Validation<R> Apply<T, R>(this Validation<Func<T, R>> valF, Validation<T> valT)  // harvesting errors
      {
         return valF.Match(
            Valid: (f) => valT.Match
            (
               Valid: (t) => Valid(f(t)),
               Invalid: (err) => Invalid(err)
            ),
            Invalid: (errF) => valT.Match(
               Valid: (_) => Invalid(errF),
               Invalid: (errT) => Invalid(errF.Concat(errT))  // <----------harvesting errors
            )
         );
      }

      public static Validation<RR> SelectMany<T, R, RR>(this Validation<T> @this, Func<T, Validation<R>> bind, Func<T, R, RR> project)  // fail-fast errors
      {
         return @this.Match(
                  Invalid: (err) => Invalid(err),
                  Valid: (t) => bind(t).Match(
                     Invalid: (err) => Invalid(err),
                     Valid: (r) => Valid(project(t, r))));
      }
   }
}
