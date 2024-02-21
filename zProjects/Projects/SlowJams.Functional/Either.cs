using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;

namespace Slowjams.Functional;

using static F;

public static partial class F
{
   public static Either.Left<L> Left<L>(L l) => new Either.Left<L>(l);
   public static Either.Right<R> Right<R>(R r) => new Either.Right<R>(r);
}

public struct Either<L, R>
{
   private L? Left { get; }
   private R? Right { get; }

   private bool IsRight { get; }
   private bool IsLeft => !IsRight;

   internal Either(L left)
   {
      (IsRight, Left, Right) = (false, left ?? throw new ArgumentNullException(nameof(left)), default);
   }

   internal Either(R right)
   {
      (IsRight, Left, Right) = (true, default, right ?? throw new ArgumentNullException(nameof(right)));
   }

   public static implicit operator Either<L, R>(L left) => new Either<L, R>(left);
   public static implicit operator Either<L, R>(R right) => new Either<L, R>(right);

   public static implicit operator Either<L, R>(Either.Left<L> left) => new Either<L, R>(left.Value);
   public static implicit operator Either<L, R>(Either.Right<R> right) => new Either<L, R>(right.Value);

   public TR Match<TR>(Func<L, TR> Left, Func<R, TR> Right)
   {
      return IsLeft ? Left(this.Left!) : Right(this.Right!);
   }
}

public static class Either
{
   public struct Left<L>
   {
      internal L Value { get; }
      internal Left(L value) { Value = value; }

      public override string ToString() => $"Left({Value})";

      // no need of Map and Bind
   }

   public struct Right<R>
   {
      internal R Value { get; }
      internal Right(R value) { Value = value; }

      public override string ToString() => $"Right({Value})";

      public Right<RR> Map<RR>(Func<R, RR> f) => Right(f(Value));
      public Either<L, RR> Bind<L, RR>(Func<R, Either<L, RR>> f) => f(Value);   // R and RR can be the same
   }
}

