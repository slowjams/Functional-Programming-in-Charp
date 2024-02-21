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
   public class MakeTransferController_v2_Either : ControllerBase
   {
      // no need to define OO style interfaces
      //private IValidator<MakeTransfer> validator;
      //private IRepository<AccountState> accounts;
      // private ISwiftService swift;

      private DateTime now;
      private Regex bicRegex = new("[A-Z]{11}");

      [HttpPost]
      [Route("transfers/book")]
      public IActionResult MakeTransfer([FromBody] MakeTransfer transfer)
         => Handle(transfer).Match<IActionResult>
         (
            Left: BadRequest,
            Right: _ => Ok()
         );

      public ResultDto<Unit> MakeTransferV2([FromBody] MakeTransfer transfer)
        => Handle(transfer).ToResult();

      private Either<Error, Unit> Handle(MakeTransfer transfer)
         => Right(transfer)
            .Bind(ValidateBic)    // this Bind is from Either.Right Bind
            .Bind(ValidateDate)  // this Bind is from Either Bind
            .Bind(Save);

      private Either<Error, MakeTransfer> ValidateBic(MakeTransfer transfer)
         => bicRegex.IsMatch(transfer.Bic)
            ? transfer
            : Errors.InvalidBic;

      private Either<Error, MakeTransfer> ValidateDate(MakeTransfer transfer)
         => transfer.Date.Date > now.Date
           ? transfer
           : Errors.TransferDateIsPast;

      private Either<Error, Unit> Save(MakeTransfer cmd)
      {
         if (true)
            return Unit.Create();
         else
            return Errors.UnexpectedError;
      }
   }
}


