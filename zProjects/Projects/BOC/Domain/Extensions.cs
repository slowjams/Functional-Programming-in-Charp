using Slowjams.Functional;

namespace BOC.Domain
{
   public static class Extensions
   {
      public static ResultDto<T> ToResult<T>(this Either<Error, T> either)
      => either.Match
      (
         Left: error => new ResultDto<T>(error),
         Right: data => new ResultDto<T>(data)
      );
   }
}
