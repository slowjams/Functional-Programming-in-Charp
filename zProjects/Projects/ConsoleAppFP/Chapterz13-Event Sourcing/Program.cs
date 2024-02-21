using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using Boc.Domain.Events;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;

namespace ConsoleAppFP.Chapterz13_Event_Sourcingz
{
   public static class Program13
   {
      public static void Main_13(string[] args)
      {

      }

      //-------------------------------------------------------V
      public static Option<T> Head<T>(this IEnumerable<T> list)
      {
         if (list == null)
            return None;

         var enumerator = list.GetEnumerator();
         return enumerator.MoveNext() ? Some(enumerator.Current) : None;
      }

      public static R Match<T, R>(this IEnumerable<T> list, Func<R> Empty, Func<T, IEnumerable<T>, R> Otherwise)
      {
         return list.Head().Match(
            None: Empty, 
            Some: head => Otherwise(head, list.Skip(1)));
      }
      //-------------------------------------------------------Ʌ
   }

   //--------------------------------------------------------------V
   //public abstract record Event(Guid EntityId, DateTime Timestamp);

   //public record CreatedAccount(Guid EntityId, DateTime Timestamp, CurrencyCode Currency) : Event(EntityId, Timestamp);

   //public record FrozeAccount(Guid EntityId, DateTime Timestamp) : Event(EntityId, Timestamp);

   //public record DepositedCash(Guid EntityId, DateTime Timestamp, decimal Amount, Guid BranchId) : Event(EntityId, Timestamp);

   //public record DebitedTransfer(Guid EntityId, DateTime Timestamp,
   //                              string Beneficiary, string Iban, string Bic,
   //                              decimal DebitedAmount, string Reference) : Event(EntityId, Timestamp);
   //--------------------------------------------------------------Ʌ

   //-------------------------V
   public static class Account
   {
      public static AccountState Create(CreatedAccount evt)  // CreatedAccount is a special case because there is no prior state
      {
         return new AccountState(Currency: evt.Currency, Status: AccountStatus.Active);
      }

      public static AccountState Apply(this AccountState acc, Event evt)  // doesn't need to handle CreatedAccount because it is special
      {
         return evt switch
         {
            DepositedCash e => acc with { Balance = acc.Balance + e.Amount },
            DebitedTransfer e => acc with { Balance = acc.Balance - e.DebitedAmount },
            FrozeAccount _ => acc with { Status = AccountStatus.Frozen },
            _ => throw new InvalidOperationException()
         };
      }

      public static Option<AccountState> From(IEnumerable<Event> history)  // first event has to be CreatedAccount
      {
         return history.Match  // IEnumerable<T>.match call Head() internally (a little bit awkward since it is not generic, only fits even sourcing scenario) 
         (
            Empty: () => None,
            Otherwise: (created, otherEvents) => 
               Some(
                  otherEvents.Aggregate(seed: Account.Create((CreatedAccount)created), func: (state, evt) => state.Apply(evt))
               )
         );
      }
   }
   //-------------------------Ʌ

   //-----------------------------------------------------------------------------------------------------V
   public sealed record AccountState(CurrencyCode Currency, AccountStatus Status = AccountStatus.Requested,
                                     decimal Balance = 0m, decimal AllowedOverdraft = 0m);

   public enum AccountStatus { Requested, Active, Frozen, Dormant, Closed }

   //public struct CurrencyCode
   //{
   //   string Value { get; }
   //   public CurrencyCode(string value) => Value = value;

   //   public static implicit operator string(CurrencyCode c) => c.Value;
   //   public static implicit operator CurrencyCode(string s) => new(s);

   //   public override string ToString() => Value;
   //}
   //-----------------------------------------------------------------------------------------------------Ʌ
}