using System;
using System.Collections.Generic;
using System.Linq;
using LaYumba.Functional;
using NUnit.Framework;
using static LaYumba.Functional.F;
using static ConsoleAppFP.Chapter9_Structuring.ValidationStrategies;
//using Slowjams.Functional;
//using static Slowjams.Functional.F;
//using Unit = System.ValueTuple;
//using SlowJams.Functional;

namespace ConsoleAppFP.Chapter9_Structuring
{
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
                         .Bind(v => v.Match(errors => Some(errors), _ => None));   // .Bind(Func<Validation<T>, Option<IEnumerable<Error>>>)
                               // v is Validation<T>                               // Some wraps errors when "invalid", and None when "valid" interestingly the reason is you want to retireve errors,
            // public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> list, Func<T, Option<R>> func)
            return errors.ToList().Count == 0? Valid(t) : Invalid(errors.Flatten());
         };
      }
   }

   public delegate Validation<T> Validator<T>(T t);

   //--------------------------------------------------VV
   public static partial class ValidationStrategiesTest
   {
      // mock validation rules
      static readonly Validator<int> Success = i => Valid(i);
      static readonly Validator<int> Failure = _ => Error("Invalid");
      //

      //-----------------------V
      public class FailFastTest
      {
         [Test]
         public void WhenAllValidatorsSucceed_ThenSucceed()
         {
            Assert.AreEqual(actual: FailFast(List(Success, Success))(1), expected: Valid(1));
         }

         [Test]
         public void WhenNoValidators_ThenSucceed()
         {
            Assert.AreEqual(actual: FailFast(List<Validator<int>>())(1), expected: Valid(1));
         }

         [Test]
         public void WhenOneValidatorFails_ThenFail()
         {
            FailFast(List(Success, Failure))(1).Match(
               Valid: (_) => Assert.Fail(),
               Invalid: (errs) => Assert.AreEqual(1, errs.Count()));
         }

         [Test]
         public void WhenSeveralValidatorsFail_ThenFail()
         {
            FailFast(List(Success, Failure, Failure, Success))(1).Match(
               Valid: (_) => Assert.Fail(),
               Invalid: (errs) => Assert.AreEqual(1, errs.Count())); // only the first error is returned
         }        
      }
      //-----------------------Ʌ

      //----------------------------V
      public class HarvestErrorsTest
      {
         [Test]
         public void WhenAllValidatorsSucceed_ThenSucceed()
         {
            Assert.AreEqual(
               actual: HarvestErrors(List(Success, Success))(1), 
               expected: Valid(1));
         }

         [Test]
         public void WhenNoValidators_ThenSucceed()
         {
            Assert.AreEqual(
               actual: HarvestErrors(List<Validator<int>>())(1),
               expected: Valid(1));
         }

         [Test]
         public void WhenOneValidatorFails_ThenFail()
         {
            HarvestErrors(List(Success, Failure))(1).Match(
               Valid: (_) => Assert.Fail(), 
               Invalid: (errs) => Assert.AreEqual(1, errs.Count()));
         }

         [Test]
         public void WhenSeveralValidatorsFail_ThenFail()
         {
            HarvestErrors(List(Success, Failure, Failure, Success))(1).Match(
               Valid: (_) => Assert.Fail(),
               Invalid: (errs) => Assert.AreEqual(2, errs.Count()));  // all errors are returned
         }          
      }
      //----------------------------Ʌ
   }
   //--------------------------------------------------ɅɅ
}


