using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;

namespace Slowjams.Functional;

using static Slowjams.Functional.F;

public static class BindE
{
   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, Option<R>> f)
   {
      foreach (T t in ts)
      {
         foreach (R r in f(t).AsEnumerable())
         {
            yield return r;
         }
      }

      //ts.Bind(t => f(t).AsEnumerable());
   }

   public static IEnumerable<R> Bind<T, R>(this Option<T> opt, Func<T, IEnumerable<R>> f)
   {
      //return opt.AsEnumerable().Bind(t => f(t));
      return opt.AsEnumerable().Bind(f);
   }

   //  Option<Int> -> Option<Age>
   // (Option<Int>, int => Option<Age>) -> Option<Age>
   public static Option<Age> Bind<Int, Age>(this Option<Int> optT, Func<int, Option<Age>> f) => None;

   public static Option<R> Bind<T, R>(this Option<T> optT, Func<T, Option<R>> f)
   {
      return optT.Match(
         () => None,
         (t) => f(t)
      );
   }

   // IEnumerable<Neighbor> -> IEnumerable<Pet>
   // (IEnumerable<Neighbor>, Neighbor => IEnumerable<Pet>) -> IEnumerable<Pet>
   public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> f)
   {
      foreach (T t in ts)
      {
         foreach (R r in f(t))
         {
            yield return r;
         }
      }
   }

   public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> list)
         => list.SelectMany(x => x);

   //public static Option<R> Map<T, R>(this Option<T> opt, Func<T, R> f) => opt.Bind(t => Some(f(t)));

   //public static IEnumerable<R> Map<T, R>(this IEnumerable<T> ts, Func<T, R> f) => ts.Bind(t => CustomType.List(f(t)));
}

public static class E
{
   public static IEnumerable<R> Map<T, R>(this IEnumerable<T> ts, Func<T, R> f)
   {
      return ts.Select(f);
   }

   public static Option<R> Map<T, R>(this Option<T> optT, Func<T, R> f)
   {
      return optT.Match(
         () => None,
         (t) => Some(f(t))
      );
   }

   public static Option<Unit> Map<T, R>(this Option<T> optT, Action<T> action)
   {
      return Map(optT, action.ToFunc());
   }

   public static Option<Unit> Map<T>(this Option<T> optT, Action<T> action)
   {
      return Map(optT, action.ToFunc());
   }

   public static Option<T> Where<T>(this Option<T> optT, Func<T, bool> pred)
   {
      return optT.Match
      (
         () => None,
         (t) => pred(t) ? optT : None
      );
   }

   //public static Option<Unit> Map<T>(this Option<T> optT, Action<T> action)
   //{
   //   return optT.Match(
   //      () => None,
   //      (a) => Some(action.ToFunc()(a))
   //   );
   //}

   public static Option<Unit> ForEach<T>(this Option<T> opt, Action<T> action)
   {
      return Map(opt, action.ToFunc());
   }

   public static IEnumerable<Unit> ForEach<T>(this IEnumerable<T> ts, Action<T> action)
   {
      return ts.Map(action.ToFunc()).ToImmutableList();
   }
}

public static class ActionExt
{
   public static Func<Unit> ToFunc(this Action action) => () => { action(); return default; };

   public static Func<T, Unit> ToFunc<T>(this Action<T> action) => (t) => { action(t); return default; };

   public static Func<NoneType> ToFuncNoneType(this Action action) => () => { action(); return default; };
}