using Slowjams.Functional;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;

namespace ConsoleAppFP.Chapter4_Function_Signatures_and_Types
{
   internal class Program4
   {
      static void Main4(string[] args)
      {
         var contents = Instrumentation.Time("writing to file.txt", () => File.AppendAllText("file.txt", "New content", Encoding.UTF8));
      }
   }

   public static class Instrumentation
   {
      public static T Time<T>(string op, Func<T> f)
      {
         var sw = new Stopwatch();
         sw.Start();
         T t = f();
         sw.Stop();
         Console.WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
         return t;
      }

      public static Unit Time(string op, Action act) => Time<Unit>(op, act.ToFunc());
   }

   public static class Instrumentation2
   {
      public static T Time<T>(string op, Func<T> f)
      {
         var sw = new Stopwatch();
         sw.Start();
         T t = f();
         sw.Stop();
         Console.WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
         return t;
      }

      public static NoneType Time(string op, Action act) => Time<NoneType>(op, act.ToFuncNoneType());
   }


   public enum Risk { Low, Medium, High }

   public struct Age
   {
      private int Value { get; }

      public Age(int value)
      {
         if (!IsValid(value))
            throw new ArgumentException($"{value} is not a valid age");

         Value = value;
      }

      private static bool IsValid(int age) => 0 <= age && age < 120;

      public static bool operator <(Age a, Age b) => a.Value < b.Value;
      public static bool operator >(Age a, Age b) => a.Value > b.Value;

      public static bool operator <(Age a, int i) => a < new Age(i);  // note that it can't be `a.Value < i`

      public static bool operator >(Age a, int i) => a > new Age(i);

      public override string ToString() => Value.ToString();
   }
}
//public static class InstrumentationDuplicatedLogic
//{
//    public static T Time<T>(string op, Func<T> f)
//    {
//        var sw = new Stopwatch();
//        sw.Start();
//        T t = f();
//        sw.Stop();
//        Console.WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
//        return t;
//    }

//    public static void Time(string op, Action act)
//    {
//        var sw = new Stopwatch();
//        sw.Start();
//        act();
//        sw.Stop();
//        Console.WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");

//    }
//}


