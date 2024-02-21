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
   public class MakeTransferController_v3_Validation_Exceptional : ControllerBase
   {
      private DateTime now;
      private Regex bicRegex = new("[A-Z]{11}");

      private ILogger<MakeTransferController_v3_Validation_Exceptional> logger;

      [HttpPost]
      [Route("transfers/book")]
      public IActionResult MakeTransfer([FromBody] MakeTransfer transfer)
        => Handle(transfer).Match<IActionResult>   // IActionResult can be inferred, just for demo purpose
        (
           Invalid: BadRequest,   // Validation's IEnumerable<Error> is passed as an argument to BadRequest(object error)
           Valid: result => result.Match
           (
              OnFaulted,
              _ => Ok()   // _ is Unit
           )
        );

      private IActionResult OnFaulted(Exception ex)
      {
         logger.LogError(ex.Message);
         return StatusCode(500, Errors.UnexpectedError);
      }

      private Validation<Exceptional<Unit>> Handle(MakeTransfer transfer)
         => Validate(transfer)
            .Map(Save);

      private Validation<MakeTransfer> Validate(MakeTransfer transfer)   // Top-level validation function combining various validation rules
         => ValidateBic(transfer)
            .Bind(ValidateDate);
      private Validation<MakeTransfer> ValidateBic(MakeTransfer transfer)
         => bicRegex.IsMatch(transfer.Bic)
          ? transfer
          : Errors.InvalidBic;

      private Validation<MakeTransfer> ValidateDate(MakeTransfer transfer)
         => transfer.Date.Date > now.Date
            ? transfer
            : Errors.TransferDateIsPast;

      private Exceptional<Unit> Save(MakeTransfer cmd)
      {
         try
         {
            // ... using third-party API that throws an exception is wrapped in try statement
         }
         catch (Exception ex)
         {
            return ex;
         }

         return Unit.Create();
      }
   }
}