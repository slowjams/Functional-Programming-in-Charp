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
   public class Program8
   {    
      public static void Main8(string[] args)
      {
         var zzzzz = Unit();
         var a = Right(12);
         var b = Left("oops");

         var c = Render(Right(12d));
         var d = Render(Left("oops"));
      }

      public static string Render(Either<string, double> val)
      {
         return val.Match                         // public TR Match<TR>(Func<L, TR> Left, Func<R, TR> Right)
         (
            Left: l => $"Invalid value: {l}",     // TR is string here
            Right: r => $"The result is: {r}"
         );
      }

      public static Either<string, double> Calc(double x, double y)
      {
         if (y == 0)
            return "y cannot be 0";
         if (x != 0 && Math.Sign(x) != Math.Sign(y))
            return "x / y cannot be negative";

         return Math.Sqrt(x / y);
      }
   }

   class Interview_Example_Option
   {
      Func<Candidate, bool> IsEligible;
      Func<Candidate, Option<Candidate>> Interview;

      Option<Candidate> FirstRound(Candidate c)
         => Some(c)
            .Where(IsEligible)
            .Bind(Interview);
   }

   class Interview_Example_Either
   {
      Func<Candidate, bool> IsEligible;
      Func<Candidate, Either<Rejection, Candidate>> Interview;

      Either<Rejection, Candidate> CheckEligibility(Candidate c)
      {
         if (IsEligible(c)) return c;
         return new Rejection("Not eligible");
      }

      Either<Rejection, Candidate> FirstRound(Candidate c)
         => Right(c)
            .Bind(CheckEligibility)
            .Bind(Interview);
   }

   record Candidate { }
   record Rejection(string Reason);
}