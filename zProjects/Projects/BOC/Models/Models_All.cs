using Slowjams.Functional;
using static Slowjams.Functional.F;
using System;

namespace BOC.Models
{
   public record AccountState(decimal Balance);

   public static class Account
   {
      public static Option<AccountState> Debit(this AccountState current, decimal amount)
      {
         return current.Balance < amount ? None : Some(new AccountState(current.Balance - amount));
      }
   }

   public record MakeTransfer(Guid DebitedAccountId, decimal Amount, string Bic, DateTime Date);
   // --------------------------------------------------------------------------------Ʌ

   public interface IRepository<T>
   {
      Option<T> Get(Guid id);
      void Save(Guid id, T t);
   }

   public interface ISwiftService
   {
      void Wire(MakeTransfer transfer, AccountState account);
   }

   public interface IValidator<T>
   {
      bool IsValid(T t);
   }
}
