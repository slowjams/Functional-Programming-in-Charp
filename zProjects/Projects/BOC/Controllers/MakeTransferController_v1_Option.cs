using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOC.Models;
//using LaYumba.Functional;
//using static LaYumba.Functional.F;
using Slowjams.Functional;
using static Slowjams.Functional.F;
using BOC.Domain;
using Unit = System.ValueTuple;
using System.Text.RegularExpressions;
using SlowJams.Functional;

namespace BOC.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class MakeTransferController_Option : ControllerBase
   {
      private IValidator<MakeTransfer> validator;
      private IRepository<AccountState> accounts;
      private ISwiftService swift;

      private DateTime now;
      private Regex bicRegex = new("[A-Z]{11}");

      //---------OO VS FP Option-------------------------V
      public void MakeTransferOO([FromBody] MakeTransfer transfer)      // OO style
      {
         if (validator.IsValid(transfer))
            Book(transfer);
      }

      [HttpPost]
      [Route("transfers/book")]
      public void MakeTransfer([FromBody] MakeTransfer transfer)   // FP, Option style, better than OO
      {
         Some(transfer)
         .Where(validator.IsValid)
         .ForEach(Book);
      }

      private void Book(MakeTransfer transfer)
      {
         Option<AccountState> optAccountState = accounts.Get(transfer.DebitedAccountId)
            .Bind(account => account.Debit(transfer.Amount));

         optAccountState.ForEach(account =>
         {
            accounts.Save(transfer.DebitedAccountId, account);
            swift.Wire(transfer, account);
         });
      }
   }   
}