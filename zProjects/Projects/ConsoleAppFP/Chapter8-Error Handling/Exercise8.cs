using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using SlowJams.Functional;
//using static SlowJams.Functional.F;
using LaYumba.Functional;
using static LaYumba.Functional.F;

namespace ConsoleAppFP.Chapter8_ErrorHandling
{
   public class Exercise
   {
      public static void Mainz(string[] args)
      {
         Console.ReadLine();
      }

      public static Exceptional<T> TryRun<T>(Func<T> func)
      {
         try
         {
            return func();
         }
         catch (Exception ex)
         {
            return ex;
         }
      }

      public static Either<L, R> Safely<L, R>(Func<R> func, Func<Exception, L> left)
      {
         try
         {
            return func();
         }
         catch (Exception ex)
         {
            return left(ex);
         }
      }
   }

   public static class ExerciseExtension
   {
      public static Option<R> ToOption<L, R>(this Either<L, R> either)
      {
         return either.Match(l => None, r => Some(r));
      }

      public static Either<L, R> ToEither<L, R>(this Option<R> option, Func<L> left)
      {
         return option.Match<Either<L, R>>(() => left(), r => r);
      }
   }

   public static class Int
   {
      public static Option<int> Parse(string s) => int.TryParse(s, out int result) ? Some(result) : None;
   }
}