using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSourcing.Transition;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Microsoft.AspNetCore.Mvc;
using Boc.Domain;
using Boc.Commands;
using Boc.Domain.Events;

namespace ConsoleAppFP.Chapterz13_Event_Sourcing.Transition
{
   public class EventSourcingController : ControllerBase
   {
      Func<CreateAccountWithOptions, Validation<CreateAccountWithOptions>> validate;
      Func<Guid> generateId;
      Action<Event> saveAndPublish;

      public IActionResult CreateInitialized([FromBody] CreateAccountWithOptions cmd)
      {
         return 
            validate(cmd)
               .Bind(Initialize)
               .Match<IActionResult>
               (
                  Invalid: errs => BadRequest(new { Errors = errs }),
                  Valid: id => Ok(id)
               );
      }

      private Validation<Guid> Initialize(CreateAccountWithOptions cmd)  // note that there is reason to call this method "Initialize"
      {                                                                  // because the is no replay of historical events
         Guid id = generateId();
         DateTime now = DateTime.UtcNow;

         var create = new CreateAccount
         (
            Timestamp: now,
            AccountId: id,
            Currency: cmd.Currency
         );

         var depositCash = new AcknowledgeCashDeposit
         (
            Timestamp: now,
            AccountId: id,
            Amount: cmd.InitialDepositAccount,
            BranchId: cmd.BranchId
         );

         var setOverdraft = new SetOverdraft
         (
             Timestamp: now,
             AccountId: id,
             Amount: cmd.AllowedOverdraft
         );

         /* just to demo two `from` clauses work too and easy to understand with SelectMany call
         Transition<AccountState, IEnumerable<Event>> _ =  
            from f1 in Account.Create(create)
            from f2 in Account.Deposit(depositCash)
            select List<Event>(f1, f2);
         */

         Transition<AccountState, IEnumerable<Event>> transitions =   // check chapter 10 if you forget how multiple from clauses used and how "unrelated" things e1, e2, e3
            from e1 in Account.Create(create)                         // (by "unrelated", I mean you don't need e1 to generate e2 or e2 to generate e3) work together
            from e2 in Account.Deposit(depositCash)
            from e3 in Account.SetOverdraft(setOverdraft)
            select List<Event>(e1, e2, e3);

         return transitions(default(AccountState))     // Validation<(IEnumerable<Event>, AccountState)>
            .Do(t => t.Item1.ForEach(saveAndPublish))  // still return Validation<(IEnumerable<Event>, AccountState)> because Do return @this
            .Map(_ => id);   // use discard as we just want wrap Guid(id) into Validation
      }
   }

   public record CreateAccountWithOptions
   (
      DateTime Timestamp,

      Boc.Domain.CurrencyCode Currency,  // remove Boc.Domain when copy to VS code
      decimal InitialDepositAccount,
      decimal AllowedOverdraft,
      Guid BranchId
   ) : Command(Timestamp);
}