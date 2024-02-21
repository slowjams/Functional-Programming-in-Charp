using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using EventSourcing.Transition;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain.Events;
using Boc.Commands;

namespace Boc.Domain
{
   public static class Account
   {
      public delegate Validation<T> Validator<T>(T t);  // Transition is actually a Validator

      public delegate Validation<(T, St)> Transition_<St, T>(St state);  // easy to look to be referenced purpose only   

      #region handle commands
      //------------------------------------------------------------------------------V
      public static Transition<AccountState, CreatedAccount> Create(CreateAccount cmd)
      {
         return _ =>  // use discard here because CreateAccount is special as it doesn't have prior state
         {
            CreatedAccount evt = cmd.ToEvent();
            AccountState newState = evt.ToAccount();
            return (evt, newState);
         };
      }

      public static Transition<AccountState, DepositedCash> Deposit(AcknowledgeCashDeposit cmd)
      {
         return accountSt =>
         {
            if (accountSt.Status != AccountStatus.Active)
               return Errors.AccountNotActive;

            DepositedCash evt = cmd.ToEvent();
            AccountState newState = accountSt.Apply(evt);

            return (evt, newState);
         };
      }

      public static Transition<AccountState, AlteredOverdraft> SetOverdraft(SetOverdraft cmd)
      {
         return accountSt =>
         {
            AlteredOverdraft evt = cmd.ToEvent(cmd.Amount - accountSt.AllowedOverdraft);
            AccountState newState = accountSt.Apply(evt);

            return (evt, newState);
         };
      }
      //------------------------------------------------------------------------------Ʌ

      public static (Event Event, AccountState NewState) Debit_Old(this AccountState currentState, MakeTransfer transfer)
      {
         DebitedTransfer evt = transfer.ToEvent();  // can use `var` here, just for readability to use explicit event type here
         AccountState newState = currentState.Apply(evt);

         return (evt, newState);
      }

      public static Validation<(Event Event, AccountState NewState)> Debit(this AccountState currentState, MakeTransfer transfer)
      {
         if (currentState.Status != AccountStatus.Active)
            return Errors.AccountNotActive;

         if (currentState.Balance - transfer.Amount < currentState.AllowedOverdraft)
            return Errors.InsufficientBalance;

         var evt = transfer.ToEvent();
         AccountState newState = currentState.Apply(evt);

         return (evt, newState);
      }
      #endregion

      #region apply events
      public static AccountState Create(CreatedAccount evt)  // CreatedAccount is a special case because there is no prior state
      {
         return new AccountState(Currency: evt.Currency, Status: AccountStatus.Active);
      }

      public static AccountState ToAccount(this CreatedAccount evt)  // same as above, but use extension method
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
         return history.Match  // IEnumerable<T>.match call Head() internally (a little bit awkward since it doesn't look generic, but it actully make sense because of the return type) 
         (
            Empty: () => None,
            Otherwise: (created, otherEvents) =>
               Some(
                  otherEvents.Aggregate(seed: Account.Create((CreatedAccount)created), func: (state, evt) => state.Apply(evt))
               )
         );
      }
      #endregion
   }

   //-----------------------------------------------------------------------------------------------------V
   public sealed record AccountState(CurrencyCode Currency, AccountStatus Status = AccountStatus.Requested,
                                     decimal Balance = 0m, decimal AllowedOverdraft = 0m);

   public enum AccountStatus { Requested, Active, Frozen, Dormant, Closed }

   public struct CurrencyCode
   {
      string Value { get; }
      public CurrencyCode(string value) => Value = value;

      public static implicit operator string(CurrencyCode c) => c.Value;
      public static implicit operator CurrencyCode(string s) => new(s);

      public override string ToString() => Value;
   }
   //-----------------------------------------------------------------------------------------------------Ʌ
}