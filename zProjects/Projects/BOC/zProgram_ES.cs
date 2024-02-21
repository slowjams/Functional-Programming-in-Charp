using System;
using System.Threading.Tasks;
//using Slowjams.Functional;
using Unit = System.ValueTuple;
//using SlowJams.Functional;
//using static Slowjams.Functional.F;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Http.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Boc.Domain;
using Boc.Commands;
using Boc.Domain.Events;

namespace Boc
{
   public static class Program_Event_Sourcing
   {
      public static void ConfigureMakeTransferEndpoint
         (WebApplication app, Func<MakeTransfer, Validation<MakeTransfer>> validate, Func<Guid, Option<AccountState>> getAccount, Action<Event> saveAndPublish)
      {
         app.MapPost("/Transfer/Make", (MakeTransfer cmd) =>
         {
            validate(cmd)
               .Bind(t => getAccount(t.DebitedAccountId).ToValidation($"No account found for {t.DebitedAccountId}"))
               .Bind(acc => acc.Debit(cmd))   // acc is AccountState
               .Do(result => saveAndPublish(result.Event))
               .Match(
                  Invalid: errors => BadRequest(new { Errors = errors }),
                  Valid: result => Ok(new {result.NewState.Balance})
               );
         });
      }

      public static void ConfigureMakeTransferEndpoint_Old(WebApplication app, Func<Guid, AccountState> getAccount, Action<Event> saveAndPublish)
      {
         app.MapPost("/Transfer/Make", (MakeTransfer cmd) =>
         {
            AccountState account = getAccount(cmd.DebitedAccountId);
            var (evt, newState) = account.Debit_Old(cmd);

            saveAndPublish(evt);

            return Ok(new { newState.Balance });
         });
      }
   } 
}
