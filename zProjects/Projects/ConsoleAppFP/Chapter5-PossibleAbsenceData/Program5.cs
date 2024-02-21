using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slowjams.Functional;
using static Slowjams.Functional.F;

namespace ConsoleAppFP.Chapter5_PossibleAbsenceData
{
   public class Program5
   {
      static void Main5(string[] args)
      {
         Option<string> _ = None;
         Option<string> john = Some("John");

         Console.WriteLine(Greet(Some("John")));
         Console.ReadLine();

         //Enum.Parse<DayOfWeek>("Friday");
         var zzz = "Friday".Parse<DayOfWeek>();
      }

      static string Greet(Option<string> greetee)
      {
         return greetee.Match(
            None: () => "sorry, who?",
            Some: (name) => $"Hello, {name}");
      }
   }

   public static class Exercise
   {
      public static Option<T> Parse<T>(this string source) where T : struct
      {
         return Enum.TryParse(source, out T t) ? Some(t) : None;
      }

      public static Option<T> Lookup<T>(this IEnumerable<T> source, Func<T, bool> predicate)
      {
         foreach(T t in source)
         {
            if(predicate(t))
               return Some(t);
         }
         return None;
      }
   }

   public struct Age
   {
      private int Value { get; }

      private Age(int value) => Value = value;  // constructor is private, we can only get an instance from Create static method

      private static bool IsValid(int age) => 0 <= age && age < 120;

      public static Option<Age> Create(int age) => IsValid(age) ? Some(new Age(age)) : None;
   }

   public enum DayOfWeek
   {
      Monday,
      Tuesday,
      Wednesday,
      Thursday,
      Friday,
      Saturday,
      Sunday
   }
}
