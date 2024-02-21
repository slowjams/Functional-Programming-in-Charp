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

namespace ConsoleAppFP.Chapter7_FunctionComposition
{
   public class Program7
   {
      static void Main7(string[] args)
      {

      }

   }

   public record AccountState(decimal Balance);

   public static class Account
   {
      public static Option<AccountState> Debit(this AccountState current, decimal amount)
      {
         return current.Balance < amount ? None : Some(new AccountState(current.Balance - amount));
      }
   }

   // -------------------------------------------------------------------------------------
}