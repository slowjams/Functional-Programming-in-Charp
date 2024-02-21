using System;
using System.Threading.Tasks;
using BOC.Models;
using Slowjams.Functional;
using Unit = System.ValueTuple;
using SlowJams.Functional;
using static Slowjams.Functional.F;
using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Http.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using BOC.Helpers;
using BOC.Domain;

namespace BOC
{
   public static class Program_FP
   {     
      public async static Task Run()
      {
         WebApplication app = WebApplication.Create();

         Func<MakeTransfer, IResult> handleSaveTransfer = ConfigureSaveTransferHandler(app.Configuration);

         app.MapPost("/Transfer/Future", handleSaveTransfer);

         await app.RunAsync();
      }

      public static Func<MakeTransfer, IResult> ConfigureSaveTransferHandler(IConfiguration config)
      {
         ConnectionString connString = config.GetSection("ConnectionString").Value;
         
         SqlTemplate InsertTransferSql = "INSERT ...";

         Func<object, Exceptional<Unit>> save = connString.TryExecute(InsertTransferSql);

         Validator<MakeTransfer> validate = Validation.DateNotPast(() => DateTime.UtcNow);   // "bake" Func<DateTime> () => DateTime.UtcNow into returned delegate

         return HandleSaveTransfer(validate, save);  // "bake" thoese delegates into another returned delegate
      }

      public static Func<MakeTransfer, IResult> HandleSaveTransfer(Validator<MakeTransfer> validate,
                                                                  Func<MakeTransfer, Exceptional<Unit>> save)
      {
         return transfer => validate(transfer).Map(save).Match
         (
            err => BadRequest(err),
            result => result.Match(_ => StatusCode(StatusCodes.Status500InternalServerError), _ => Ok())
         );
      }
   }

   public delegate Validation<T> Validator<T>(T t);

   public static class Validation
   {
      public static Validator<MakeTransfer> DateNotPast(Func<DateTime> clock)
      {
         return transfer => transfer.Date.Date < clock().Date ? Errors.TransferDateIsPast : Valid(transfer);
      }
   }
}
