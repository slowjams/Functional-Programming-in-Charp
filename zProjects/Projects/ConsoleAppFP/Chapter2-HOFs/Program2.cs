using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFP
{
   class Program2
   {
      static void Main2(string[] args)
      {
         int? maybe = 12;

         if (maybe is var number)
         {
            Console.WriteLine($"The nullable int 'maybe' has the value {number}");
         }

         var divideInt = (int x, int y) => x / y;
         var divideByInt = divideInt.SwapArgs();

         var divideDouble = (double x, double y) => x / y;
         var divideByDouble = divideDouble.SwapArgs();

         var divideMix = (int x, double y) => x / y;
         var divideByMix = divideMix.SwapArgs();

         Enumerable.Range(1, 20).Where(isMod(3));

         Person husband = new("John", "Jackson");
         var dfdf = husband.LastName;
         //Person wife = husband with { FirstName = "Jenny" };

         (string aa, string bb) = husband;
         var (rr, tt) = husband;

         Person2 mm = new("Tom");

         //if (mm is (var name) _)
         //{
         //   Console.WriteLine(name); // Output is John Citizen
         //}
      }

      static Func<int, bool> isMod(int n) => i => i % n == 0;

      static Func<T, bool> NegatePredicate<T>(Func<T, bool> pred) => t => !pred(t);

      static R Using<TDisp, R>(Func<TDisp> createDisposable, Func<TDisp, R> func) where TDisp : IDisposable
      {
         using (var disp = createDisposable())
         {
            return func(disp);
         }
      }

      public static string CheckAddress(Address address)
      {
         return address switch
         {
            UsAddress(var state) => "...",
            ("de") _ => "XXX",
            (var country) _ => "...",
         };
      }
   }

   record Address(string Country);
   record UsAddress(string State) : Address("us");

   public record Person(string FirstName, string LastName)
   {
      //private string FirstName { get; } = FirstName;
   }

   public record Person2(string FirstName)
   {
      //private string FirstName { get; } = FirstName;
   }

   public static class HOFs
   {
      public static Func<T2, T1, R> SwapArgs<T1, T2, R>(this Func<T1, T2, R> f) => (t1, t2) => f(t2, t1);
   }

   static class StringExt
   {
      public static string ToSentenceCase(this string s) => s == string.Empty ? string.Empty : char.ToUpperInvariant(s[0]) + s.ToLower()[1..];
   }

   class ListFormatterImpure
   {
      int counter;
      string PrependCounter(string s) => $"{++counter}. {s}";
      public List<string> Format(List<string> list) => list.Select(StringExt.ToSentenceCase).Select(PrependCounter).ToList();
   }

   static class ListFormatter
   {
      public static List<string> Format(List<string> list)
      {
         var left = list.Select(StringExt.ToSentenceCase);
         var right = Enumerable.Range(1, list.Count);
         var zipped = Enumerable.Zip(left, right, (str, count) => $"{count}. {str}");
         return zipped.ToList();
      }
   }
}
