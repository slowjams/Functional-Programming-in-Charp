using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;
using Boc.Domain.Events;

namespace Boc.Commands
{
   public abstract record Command(DateTime Timestamp);  // not sure why the author didn't put AccountId here

   public record CreateAccount  // command uses normal tense
   (
      DateTime Timestamp,
      Guid AccountId,
      CurrencyCode Currency
   ) : Command(Timestamp)
   {
      public CreatedAccount ToEvent() => new CreatedAccount  // event uses past tense XXXedXXX
      (
         EntityId: this.AccountId,
         Timestamp: this.Timestamp,
         Currency: this.Currency
      );
   }

   public record FreezeAccount(DateTime Timestamp, Guid AccountId) : Command(Timestamp)
   {
      public FrozeAccount ToEvent()
      {
         return new FrozeAccount(EntityId: this.AccountId, Timestamp: this.Timestamp);
      }
   }

   public record MakeTransfer(Guid DebitedAccountId, string Beneficiary, string Iban, string Bic, 
                              DateTime Date, decimal Amount, string Reference, DateTime Timestamp = default) : Command(Timestamp)
   {
      // useful for testing, when you don't need all the properties to be populated
      internal static MakeTransfer Dummy
         => new(default, default!, default!, default!, default!, default!, default!);

      public DebitedTransfer ToEvent() => new
      (
         Beneficiary: this.Beneficiary,
         Bic: this.Bic,
         DebitedAmount: this.Amount,
         EntityId: this.DebitedAccountId,
         Iban: this.Iban,
         Reference: this.Reference,
         Timestamp: this.Timestamp
      );
   }

   public record AcknowledgeCashDeposit
   (
      DateTime Timestamp,
      Guid AccountId, 
      decimal Amount, 
      Guid BranchId
   ) : Command(Timestamp)
   {
      public DepositedCash ToEvent() => new DepositedCash
      (
         EntityId: this.AccountId,
         Timestamp: this.Timestamp,
         Amount: this.Amount,
         BranchId: this.BranchId
      );
   }

   public record SetOverdraft
   (
      DateTime Timestamp,
      Guid AccountId,
      decimal Amount
   ) : Command(Timestamp)
   {
      public AlteredOverdraft ToEvent(decimal by) => new AlteredOverdraft
      (
         EntityId: this.AccountId,
         Timestamp: this.Timestamp,
         By: by
      );
   }
}
