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
using SlowJams.Functional;
using BOC.Domain;
using Unit = System.ValueTuple;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using BOC.Helpers;

namespace BOC.Controllers
{
   //-----------------------------------------------
   public delegate Validation<T> Validator<T>(T t);

   public static class Validation
   {
      public static Validator<MakeTransfer> DateNotPast(Func<DateTime> clock)
      {
         return transfer => transfer.Date.Date < clock().Date ? Errors.TransferDateIsPast : Valid(transfer);
      }
   }

   public static class Program
   {
      public static void Main9(string[] args, IConfiguration config)
      {
         ConnectionString connString = config.GetSection("ConnectionString").Value;
         SqlTemplate InsertTransferSql = "INSERT ...";

         var validate = Validation.DateNotPast(clock: () => DateTime.UtcNow);
         
         var save = connString.TryExecute(InsertTransferSql);
      }
   }
   //-----------------------------------------------

   [ApiController]
   [Route("[controller]")]
   public class MakeTransferController_v4_Modularity : ControllerBase
   {
      private DateTime now;
      private Regex bicRegex = new("[A-Z]{11}");
      private ILogger<MakeTransferController_v3_Validation_Exceptional> logger;

      private Validator<MakeTransfer> validate;             // ----------------------
      private Func<MakeTransfer, Exceptional<Unit>> save;   // ----------------------

      [HttpPost]
      [Route("transfers/book")]
      public IActionResult MakeTransfer([FromBody] MakeTransfer transfer)   //---------------
        => validate(transfer).Map(save).Match<IActionResult>
        (
           Invalid: BadRequest,
           Valid: result => result.Match
           (
              Exception: OnFaulted,
              Success: _ => Ok()
           )
        );

      [HttpPost]
      [Route("transfers/book")]
      public IActionResult MakeTransferFromV3([FromBody] MakeTransfer transfer)
        => Handle(transfer).Match<IActionResult>
        (
           Invalid: BadRequest,
           Valid: result => result.Match
           (
              Exception: OnFaulted,
              Success: _ => Ok()
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

        //private Exceptional<Validation<Unit>> Handle2(MakeTransfer transfer)
        //   => Validate(transfer)
        //      .Traverse(Save);
      
        private Validation<MakeTransfer> Validate(MakeTransfer transfer)   // top-level validation function combining various validation rules
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
            // ...
         }
         catch (Exception ex)
         {
            return ex;
         }

         return Unit.Create();
      }
   }
}