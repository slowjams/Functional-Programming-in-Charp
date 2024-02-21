using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using Pet = System.String;
using System.Collections.Immutable;
using Slowjams.Functional;
using static Slowjams.Functional.F;
//using LaYumba.Functional;

namespace ConsoleAppFP.Chapter6_Patterns
{
   public class Program6
   {
      static void Main6(string[] args)
      {
         // new List<int> { 1, 2, 3 }.ForEach(Console.Write);
         var greet = (string name) => $"hello, {name}";

         Option<string> empty = None;
         Option<string> optJohn = Some("John");

         Option<string> result = empty.Map(greet);
         Option<string>  result2 = optJohn.Map(greet);

         var opt = Some("John");

         //opt.ForEach(name => Console.WriteLine($"Hello {name}"));

         opt.Map(name => $"Hello {name}").Map(Console.WriteLine);

         //opt.Map<string, Unit>(Console.WriteLine);
         opt.Map(Console.WriteLine);

         Option<string> name = Some("Enrico");
         name.Map(s => s.ToUpper()).ForEach(Console.WriteLine);

         //---------------------------------------

         Func<string, Option<Age>> parseAge = s => Int.Parse(s).Bind(Age.Create);


         Console.WriteLine("Please enter your age:");
         string input = Console.ReadLine()!;

         Option<int> optInt = Int.Parse(input);     
         Option<Age> age = optInt.Bind(Age.Create);

         Option<Age> age2 = parseAge(input);

         parseAge("26"); // => Some(26)

         var neighbors = new Neighbor[] {new (Name: "John", Pets: new Pet[] {"Fluffy", "Thor"}), new (Name: "Tim", Pets: new Pet[] {}), new (Name: "Carl", Pets: new Pet[] {"Sybil"})};

         IEnumerable<IEnumerable<Pet>> nested = neighbors.Map(n => n.Pets);

         IEnumerable<Pet> flat = neighbors.Bind(n => n.Pets);

         ImmutableList<Pet> v = flat.ToImmutableList();

         //-----------------------------------------------------

         IEnumerable<Subject> Population = new[]{ new Subject(Age.Create(33)), new Subject(None), new Subject(Age.Create(37)) };

         IEnumerable<Option<Age>> optionalAges = Population.Map(p => p.Age);

         IEnumerable<Age> statedAges = Population.Bind(p => p.Age);

      }

      // exercise
      public static Option<WorkPermit> GetWorkPermit(Dictionary<string, Employee> employees , string employeeId)
      {
         Option<Employee> employeeList = employees.Lookup(employeeId);
         Option<WorkPermit> result = employeeList.Bind(e => e.WorkPermit);
         
         return result.Where(w => !HasExpired(w));
      }

      public static double AverageYearsWorkedAtTheCompany(List<Employee> employees)
      {
         IEnumerable<double> result = employees.Bind(e => e.LeftOn.Map(leftOn => YearsBetween(e.JoinedOn, leftOn)));
         return result.Average();
      }

      public static Func<WorkPermit, bool> HasExpired => permit => permit.Expiry < DateTime.Now.Date;

      public static double YearsBetween(DateTime start, DateTime end) => (end - start).Days / 365d;
      // <
   }

   // exercise
   public record Employee(string Id, Option<WorkPermit> WorkPermit, DateTime JoinedOn, Option<DateTime> LeftOn);

   public record WorkPermit(string Number, DateTime Expiry);
   // <

   public static class ExerciseExtension
   {
      public static Option<T> Lookup<K, T>(this IDictionary<K, T> dict, K key) => dict.TryGetValue(key, out T? value) ? Some(value) : None;

      // Map : IDictionary<K, T> -> (T -> R) -> IDictionary<K, R>
      public static IDictionary<K, R> Map<K, T, R>(this IDictionary<K, T> dict, Func<T, R> f) where K : notnull  // notnull limits the type parameter to non-nullable types
      {
         var kr = new Dictionary<K, R>();

         foreach(var pair in dict)
         {
            kr[pair.Key] = f(pair.Value);
         }

         return kr;
      }

      //public static Option<R> Map<T, R>(this Option<T> opt, Func<T, R> f)
      //{
      //   return opt.Bind(t => Some(f(t)));
      //}
   }

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
      }

      public static IEnumerable<R> Bind<T, R>(this Option<T> opt, Func<T, IEnumerable<R>> f)
      {
         //return opt.AsEnumerable().Bind(t => f(t));
         return opt.AsEnumerable().Bind(f);
      }

      //  Option<Int> -> Option<Age>
      // (Option<Int>, int => Option<Age>) -> Option<Age>
      public static Option<Age> Bind<Int, Age>(this Option<Int> optT, Func<int, Option<Age>> f) => None;
      
      public static Option<R> Bind<T, R>(this Option<T> optT, Func<T, Option<R>> f) {
         return optT.Match(
            () => None,
            (t) => f(t)
         );
      }

      // IEnumerable<Neighbor> -> IEnumerable<Pet>
      // (IEnumerable<Neighbor>, Neighbor => IEnumerable<Pet>) -> IEnumerable<Pet>
      public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> f) {
         foreach(T t in ts)
         {
            foreach(R r in f(t))
            {
               yield return r;
            }
         }
      }

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

      public static Option<Unit> ForEach<T>(this Option<T> opt, Action<T> action) {
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
   }

   public static class Int
   {
      public static Option<int> Parse(string s) => int.TryParse(s, out int result) ? Some(result) : None;
   }

   public static class CustomType
   {
      public static IEnumerable<T> List<T>(params T[] items) => items.ToImmutableList();
   }

   public struct Age
   {
      private int Value { get; }

      public static Option<Age> Create(int age)
      {
         return IsValid(age) ? Some(new Age(age)) : None;
      }

      private Age(int value)
      {   // constructor is private now
         Value = value;
      }

      private static bool IsValid(int age) => 0 <= age && age < 120;
   }

   public record Subject(Option<Age> Age);

   public record Neighbor(string Name, IEnumerable<Pet> Pets);
  
}