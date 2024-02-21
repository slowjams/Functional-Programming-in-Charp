using Boc.Domain.Events;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using System;

namespace EventSourcing.Transition
{
   public delegate Validation<(T, St)> Transition<St, T>(St state);

   public static class Transition
   {
      public static Transition<St, R> Select<St, T, R>(this Transition<St, T> transition, Func<T, R> project)
      {
         return state0 => transition(state0).Map(result => (project(result.Item1), result.Item2));
      }

      public static Transition<St, R> Bind_But_Author_Call_It_SelectMany<St, T, R>(this Transition<St, T> transition, Func<T, Transition<St, R>> f)
      {
         return state0 => transition(state0).Bind(t => f(t.Item1)(t.Item2));
      }

      public static Transition<St, RR> SelectMany<St, T, R, RR>(this Transition<St, T> transition, Func<T, Transition<St, R>> bind, Func<T, R, RR> project)
      {
         return state0 => transition(state0).Bind(t => bind(t.Item1)(t.Item2).Map(r => (project(t.Item1, r.Item1), r.Item2)));
      }
   }
}