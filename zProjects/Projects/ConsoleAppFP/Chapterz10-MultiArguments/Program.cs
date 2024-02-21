using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Double = LaYumba.Functional.Double;

namespace ConsoleAppFP.Chapterz10_MultiArguments
{
   public record MyRecrod(string Other)
   {
      //public string Other { get; } 
   }

   public class TestTest
   {
      public TestTest(int a = 3)
      {

      }
   }
   
   public static class Program9
   {         
      public static void Main9(string[] args)
      {
         TestTest aa = new TestTest(7);
         
         MyRecrod a = new MyRecrod("Hi");
         

         var doubl = (int i) => i * 2;
         Option<int> _ = Some(3).Map(doubl); // => Some(6)

         Func<int, Func<int, int>> multiply = (int x) => (int y) => x * y;  // curried form

         Option<Func<int, int>> multBy3 = Some(3).Map(multiply);  // => Some(y => 3 * y))

         //-------------------------------------------V
         Option<int> optX = Some(3);
         Option<int> optY = Some(4);

         Option<int> result = optX.Map(multiply).Match
         (
            () => None, 
            (f) => optY.Match
            ( 
               () => None,
               (y) => Some(f(y))
            )
         );  // Some(12)
         //-------------------------------------------Ʌ

         //--------------------------------------------------------V
         Func<int, int, int> multiplyV2 = (int x, int y) => x * y;    // can still use non-curried form

         Option<Func<int, int>> multBy3V2 = Some(3).Map(multiplyV2);  // Map will convert non-curried form to curried form for you

         //--------------------------------------------------------Ʌ

         //------------------------------------------------------V
         // Some(12)            Option<Func<int, int>>
         Option<int> result2 = Some(3).Map(multiplyV2).Apply(Some(4));
         //------------------------------------------------------Ʌ       
      }

      public static Option<int> MultiplicationWithBind(string strX, string strY)
      {
         Func<int, int, int> multiply = (int x, int y) => x * y;

         return Int.Parse(strX).Bind(x => Int.Parse(strY).Bind<int, int>(y => multiply(x, y)));
      }

      public static void Linq()
      {
         //-------------------------------V
         Option<int> result1 =  // Some(24)
            from x in Some(12)
            select x * 2;

         Option<int> result2 =  // None
            from x in (Option<int>)None
            select x * 2;

         bool isSame = (from x in Some(1) select x * 2) == Some(1).Map(x => x * 2);  // true
         //-------------------------------Ʌ

         //----------------------------------V
         var chars = new[] { 'a', 'b', 'c' };
         var ints = new[] { 2, 3 };
         var t0 = from c in chars
                  from i in ints
                  select (c, i);

         var t1 = chars.SelectMany(c => ints, (c, i) => (c, i));  // this is the method call that the compiler will translate the above query expression into

         var t2  = chars.SelectMany(c => ints.Select(i => (c, i)));  //  performance deteriorated, another nested Select

         var t3 = chars.Bind(c => ints.Map(i => (c, i)));
         //----------------------------------Ʌ

         //--------------------------------------------V
         string s1 = "2", s2 = "3";

         // using LINQ query
         Option<int> t4 = 
            from a in Int.Parse(s1)  // you might wonder why below method is called SelectMany when there is no "many thing" like IEnumerable<T>,
            from b in Int.Parse(s2)  // it is because when there is multiple "from" clause, it will get translated into SelectMany call,
            select a + b;            // so we can get LINQ's query expression support
                                     
         // above the method invocation that the LINQ query will be converted to
         Int.Parse(s1).SelectMany(a => Int.Parse(s2), (a, b) => a + b);  // Int.Parse(s2) has no relationship to a (s1), check the source code you get the idea

         // normal method invocation
         Option<int> t5 = Int.Parse(s1).Bind(a => Int.Parse(s2).Map(b => a + b));  // check source code to see how a => Int.Parse(s2) works as a and s2 has no connection

         // using Apply
         Option<int> t6 = Some(new Func<int, int, int>((a, b) => a + b)).Apply(Int.Parse(s1)).Apply(Int.Parse(s2));
         //--------------------------------------------Ʌ
      }

      //public static Option<R> Select<T, R>(this Option<T> opt, Func<T, R> f)
      //{
      //   return opt.Map(f);
      //}

      public static void Linq2()
      {
         //--------------------------------------------V
         string s1 = "2", s2 = "3";

         Option<double> result = 
            from a in Double.Parse(s1)
            where a >= 0
            let aa = a * a

            from b in Double.Parse(s2)
            where b >= 0
            let bb = b * b
            select Math.Sqrt(aa + bb);

         //--------------------------------------------Ʌ
      }

      public static void Laws()
      {
         //  Right identity----------------V
         Option<int> opt = Some(3);

         Option<int> opt2 = opt.Bind(Some);
         // opt == opt2--------------------Ʌ

         // Right identity exampleA--------------------V
         Func<int, Option<int>> exp = x => Some(x * x);
         Option<int> r1 = Some(3).Bind(exp);
         // exp(3) = r1 = Some(9)
         //--------------------------------------------Ʌ

         // Right identity exampleB-----------------------V
         Func<int, IEnumerable<int>> f = i => Range(0, i);
         int t = 3;
         IEnumerable<int> r3 = List(t).Bind(f);
         //  f(t) = result
         //-----------------------------------------------Ʌ

         // Associativity---------------------------------------------------------------V
         Func<double, Option<double>> safeSqrt = d => d < 0 ? None : Some(Math.Sqrt(d));

         Option<string> m = Some("4");
         Option<double> r4 = m.Bind(Double.Parse)   
                              .Bind(safeSqrt);

         Option<double> r5 = m.Bind(x => Double.Parse(x).Bind(safeSqrt));

         // r4 = r5 = Some(2)
         //-----------------------------------------------------------------------------Ʌ
      }

      public static void ApplyOnFuncDirectly()
      {
         Func<int, Func<int, int>> multiply = (int x) => (int y) => x * y;

         //-------------------------------------------------V
         Option<Func<int, int>> temp = Some(3).Map(multiply);
         Option<int> _ = temp.Apply(Some(4));
         //-------------------------------------------------Ʌ

         //----------=----------------------------------------------V
         Option<int> result = 
            Some(multiply)       // Option<Func<int, Func<int, int>>>
               .Apply(Some(3))   // Option<Func<int, int>>
               .Apply(Some(4));  // Option<int>
         //---------------------------------------------------------Ʌ

         //------------------------------------------------------V
         Option<int> result2 =
             Some(multiply)  // Option<Func<int, Func<int, int>>>
                .Apply(3)
                .Apply(4);
         //------------------------------------------------------Ʌ

         //-------------------------------------------------------V
         //Func<int, int, int> multiplyV2 = (int x, int y) => x * y;
         Func<int, Func<int, int>> multiplyV2 = (int x) => (int y) => x * y; 

         Some(multiplyV2)  // Option<Func<int, Func<int, int>>>
            .Apply(3) 
            .Apply(4);
         //-------------------------------------------------------Ʌ
      }

      public static Option<R> Apply<T, R>(this Option<Func<T, R>> optF, Option<T> optT)
      {
         return optF.Match  // start with optF
         (
            () => None,
            (f) => optT.Match
            (
               () => None,
               (t) => Some(f(t))
            )
         );
      }

      public static Option<R> Apply_ImplementedUsingBind<T, R>(this Option<Func<T, R>> optF, Option<T> optT)
      {
         return optT.Bind(t => optF.Bind(f => Some(f(t))));  // start witn optT
         // return optT.Bind(t => optF.Apply(t)); 
      }

      public static Option<Func<T2, R>> Apply<T1, T2, R>(this Option<Func<T1, T2, R>> optF, Option<T1> arg)
      {
         return Apply(optF.Map(F.Curry), arg);
      }

      //--------------------------------------------------------------------------------------------V 
      public static Option<R> Map<T, R>(this Option<T> optT, Func<T, R> f)  // R can be Func<T2, RR> 
      {
         return optT.Match
         (
            () => None,
            (t) => Some(f(t))
         );
      }

      public static Option<R> Map_ImplementedUsingApply<T, R>(this Option<T> opt, Func<T, R> f)
      {
         return Some(f).Apply(opt);
      }
      //--------------------------------------------------------------------------------------------Ʌ

      public static Option<Func<T2, R>> Map<T1, T2, R>(this Option<T1> opt, Func<T1, T2, R> func)  //  takes care of currying for client who can supply non-curried function
      {
         return opt.Map(func.Curry());  // func.Curry() return Func<T1, Func<T2, R>>
      }

      //public static class Double
      //{
      //   public static Option<double> Parse(string s)
      //   {
      //      double result;
      //      return double.TryParse(s, out result)
      //         ? Some(result) : None;
      //   }
      //}
   }
}