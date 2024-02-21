﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFP.Chapter9_Structuringz
{
   public static class PartialExtension
   {
      public static Func<T2, R> Apply<T1, T2, R>(this Func<T1, T2, R> f, T1 t1)
      {
         return t2 => f(t1, t2);
      }

      public static Func<T2, T3, R> Apply<T1, T2, T3, R>(this Func<T1, T2, T3, R> f, T1 t1)
      {
         return (t2, t3) => f(t1, t2, t3);
      }
   }
}
