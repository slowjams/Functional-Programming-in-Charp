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

namespace ConsoleAppFP.Chapterz11_Representing_State_and_Chnage
{
   public static class Program9
   {
      public static void Main9(string[] args)
      {

      }    
   }

   public static class Account
   {
      public static AccountState Create(CurrencyCode ccy) => new AccountState(ccy);

      public static AccountState Activate(this AccountState account) => account with { Status = AccountStatus.Active };

      public static AccountState Add(this AccountState account, Transaction trans)
      {
         return account with
         {
            TransactionHistory
               = account.TransactionHistory.Prepend(trans)
         };
      }
   }

   public record AccountState
   (
      CurrencyCode Currency,
      AccountStatus Status = AccountStatus.Requested,
      decimal AllowedOverdraft = 0m,
      IEnumerable<Transaction> TransactionHistory = null
   )
   {
      // use a read-only property to disallow "updating" the currency of an account
      public CurrencyCode Currency { get; } = Currency;

      // use a property initializer to use an empty list rather than null
      public IEnumerable<Transaction> TransactionHistory { get; init; }
         = TransactionHistory?.ToImmutableList()
            ?? Enumerable.Empty<Transaction>();
   }

   public struct CurrencyCode
   {
      string Value { get; }
      public CurrencyCode(string value) => Value = value;

      public static implicit operator string(CurrencyCode c) => c.Value;
      public static implicit operator CurrencyCode(string s) => new(s);

      public override string ToString() => Value;
   }

   public record Transaction
   (
      decimal Amount,
      string Description,
      DateTime Date
   );

   public enum AccountStatus { Requested, Active, Frozen, Dormant, Closed }
}
