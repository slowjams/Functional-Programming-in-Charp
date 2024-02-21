using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain.Events; 
using Boc.Commands;

namespace ConsoleAppFP.Chapterz13_Event_Sourcing
{
   public record AccountStatement(int Month, int Year, decimal StartingBalance, decimal EndBalance, IEnumerable<Transaction> Transactions)
   {
      public static AccountStatement Create(int month, int year, IEnumerable<Event> events)
      {
         var startOfPeriod = new DateTime(year, month, 1);
         var endOfPeriod = startOfPeriod.AddMonths(1);
                                                                 // shoule be endOfPeriod < e.Timestamp ?
         var (eventsBeforePeriod, eventsDuringPeriod) = events.TakeWhile(e => endOfPeriod < e.Timestamp).Partition(e => e.Timestamp <= startOfPeriod);

         var startingBalance = eventsBeforePeriod.Aggregate(0m, BalanceReducer);
         var endBalance = eventsDuringPeriod.Aggregate(startingBalance, BalanceReducer);

         return new
         (
            Month: month,
            Year: year,
            StartingBalance: startingBalance,
            EndBalance: endBalance,
            Transactions: eventsDuringPeriod.Bind(CreateTransaction)
         );
      }

      public static decimal BalanceReducer(decimal bal, Event evt)
      {
         return evt switch
         {
            DepositedCash e => bal + e.Amount,
            DebitedTransfer e => bal - e.DebitedAmount,
            _ => bal
         };
      }

      public static Option<Transaction> CreateTransaction(Event evt)
      {
         return evt switch
         {
            DepositedCash e => new Transaction(CreditedAmount: e.Amount, Description: $"Deposit at {e.BranchId}", Date: e.Timestamp.Date),
            DebitedTransfer e => new Transaction(DebitedAmount: e.DebitedAmount, Description: $"Transfer to {e.Bic}/{e.Iban}; Ref: {e.Reference}", Date: e.Timestamp.Date),
            _ => None
         };
      }
   }
   public record Transaction(DateTime Date, string Description, decimal DebitedAmount = 0m, decimal CreditedAmount = 0m);

   //------------------------->>
   public static class Helpers
   {
      public static (IEnumerable<T> Passed, IEnumerable<T> Failed) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
      {
         var grouped = source.GroupBy(predicate);  // two groupings, each contains a key either true or false
         return
         (
            Passed: grouped.Where(g => g.Key).FirstOrDefault(Enumerable.Empty<T>()),
            Failed: grouped.Where(g => !g.Key).FirstOrDefault(Enumerable.Empty<T>())
         );
      }
   }
   //-------------------------<<
}