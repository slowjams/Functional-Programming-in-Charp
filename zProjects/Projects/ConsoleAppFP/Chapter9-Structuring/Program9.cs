using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using SlowJams.Functional;
//using static SlowJams.Functional.F;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Name = System.String;
using Greeting = System.String;
using PersonalizedGreeting = System.String;

namespace ConsoleAppFP.Chapter9_Structuring
{
   public class Program9
   {
      public static void Main9(string[] args)
      {
         var greet = (Greeting gr, Name name) => $"{gr}, {name}";

         Name[] names = { "Tristan", "Ivan" };

         names.Map(n => greet("Hello", n)).ForEach(Console.WriteLine);

         //----------------------------------------------------------

         var greetWith = (Greeting gr) => (Name name) => $"{gr}, {name}";

         var greetFormally = greetWith("Good evening");

         names.Map(greetFormally).ForEach(Console.WriteLine);

         //----------------------------------------------------------

         var greetInformally = greet.Apply("Hey");
         names.Map(greetInformally).ForEach(Console.WriteLine);

         
      }
   }

   //public static class PartialExtension
   //{
   //   public static Func<T2, R> Apply<T1, T2, R>(this Func<T1, T2, R> f, T1 t1)
   //   {
   //      return t2 => f(t1, t2);
   //   }

   //   public static Func<T2, T3, R> Apply<T1, T2, T3, R>(this Func<T1, T2, T3, R> f, T1 t1)
   //   {
   //      return (t2, t3) => f(t1, t2, t3);
   //   }

   //   public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> f)
   //   {
   //      return t1 => t2 => f(t1, t2);
   //   }

   //   public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(this Func<T1, T2, T3, R> f)
   //   {
   //      return t1 => t2 => t3 => f(t1, t2, t3);
   //   }

   //}

   public class TypeInference_Delegate 
   {
      private readonly string separator = ", ";

      private readonly Func<Greeting, Name, PersonalizedGreeting> GreeterField = (gr, name) => $"{gr}, {name}";

      private Func<Greeting, Name, PersonalizedGreeting> GreeterProperty => (gr, name) => $"{gr}{separator}{name}";

      public  Func<Greeting, T, PersonalizedGreeting> GreeterFactory<T>() => (gr, t) => $"{gr}{separator}{t}";

      public void Test()
      {
         GreeterFactory<Name>().Apply("Hi");
      }
   }

}