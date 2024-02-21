 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;

namespace Slowjams.Functional;

using static F;

public static class EitherExtension
{
   public static Either<L, RR> Map<L, R, RR>(this Either<L, R> either, Func<R, RR> f)
   {
      return either.Match<Either<L, RR>>
      (
         l => Left(l),
         r => Right(f(r))
      );
   }

   public static Either<LL, RR> Map<L, LL, R, RR>(this Either<L, R> either, Func<L, LL> Left, Func<R, RR> Right)
   {
      return either.Match<Either<LL, RR>>
      (
         l => F.Left(Left(l)),
         r => F.Right(Right(r))
      );
   }

   public static Either<L, RR> Bind<L, R, RR>(this Either<L, R> either, Func<R, Either<L, RR>> f)
   {
      return either.Match
      (
         l => Left(l),
         r => f(r)
      );
   }

   public static Either<L, Unit> ForEach<L, R>(this Either<L, R> either, Action<R> act)
   {
      return Map(either, act.ToFunc());
   }
   //public static Either<L, R> Where<L, R>(this Either<L, R> either, Func<R, bool> predicate)
   //{
   //   return either.Match
   //   (
   //      l => Left(l),
   //      r => predicate(r) : Right(r) ? Left(/* now what? I don't have an L */)
   //   );
   //}
}

