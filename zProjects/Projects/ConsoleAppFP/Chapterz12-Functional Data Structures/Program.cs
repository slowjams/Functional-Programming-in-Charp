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
using Double = LaYumba.Functional.Double;

namespace ConsoleAppFP.Chapterz12_Functional_Data_Structures
{
   using static LinkedList;
   public static class Program12
   {
      public static void Main12(string[] args)
      {
         
      }

      public static int Length<T>(this List<T> list)
      {
         return list.Match
         (
            () => 0,
            (_, tail) => tail.Length() + 1
         );
      }

      public static Acc Aggregate<T, Acc>(this List<T> list, Acc acc, Func<Acc, T, Acc> f)
      {
         return list.Match(() => acc, (t, ts) => Aggregate(ts, f(acc, t), f));
      }

      public static int Sum(this List<int> list) => list.Match(() => 0, (head, tail) => head + tail.Sum());

      public static List<R> Map<T, R>(this List<T> list, Func<T, R> f)
      {
         return list.Match(() => List<R>(), (t, ts) => List(f(t), ts.Map(f)));
      }

      public static List<T> Add<T>(this List<T> list, T value) => List(value, list);

      public static List<T> Tail<T>(this List<T> list)
      {
         return list.Match(() => throw new IndexOutOfRangeException(), (_, tail) => tail);
      }

      public static R Match<T, R>(this List<T> list, Func<R> Empty, Func<T, List<T>, R> Cons)
      {
         return list switch
         {
            Empty<T> => Empty(),
            Cons<T>(var t, var ts) => Cons(t, ts),
            _ => throw new ArgumentException("List can only be Empty or Cons")
         };
      }
   }

   //----------------------------------------------------------------------------------V
   public static class LinkedList
   {
      public static List<T> List<T>() => new Empty<T>();
      public static List<T> List<T>(T h, List<T> t) => new Cons<T>(h, t);
      public static List<T> List<T>(params T[] items)
      {
         return items.Reverse().Aggregate(List<T>(), (tail, head) => List(head, tail));
      }
   }

   public abstract record List<T>;
   internal sealed record Empty<T> : List<T>;
   internal sealed record Cons<T>(T Head, List<T> Tail) : List<T>;
   //----------------------------------------------------------------------------------Ʌ

   //----------------------------------------------------------------------------------V
   public abstract record Tree<T>;
   internal record Leaf<T>(T Value) : Tree<T>;
   internal record Branch<T>(Tree<T> Left, Tree<T> Right) : Tree<T>;
   //----------------------------------------------------------------------------------Ʌ

   public static class BT
   {
      public static Tree<T> Leaf<T>(T Value) => new Leaf<T>(Value);

      public static Tree<T> Branch<T>(Tree<T> Left, Tree<T> Right) => new Branch<T>(Left, Right);

      public static R Match<T, R>(this Tree<T> tree, Func<T, R> Leaf, Func<Tree<T>, Tree<T>, R> Branch)
      {
         return tree switch
         {
            Leaf<T>(T val) => Leaf(val),
            Branch<T>(var left, var right) => Branch(left, right),
            _ => throw new ArgumentException("{tree} is not a valid tree")
         };     
      }

      public static Acc Aggregate<T, Acc>(this Tree<T> tree, Acc acc, Func<Acc, T, Acc> f)
      {
         return tree.Match
         (
            Leaf: t => f(acc, t),
            Branch: (left, right) =>
            {
               var leftAcc = left.Aggregate(acc, f);
               return right.Aggregate(leftAcc, f);
            }
         );
      }

      public static Tree<T> Insert<T>(this Tree<T> tree, T value)
      {
         return tree.Match
         (
            Leaf: _ => Branch(tree, Leaf(value)),
            Branch: (left, right) => Branch(left, right.Insert(value))  // not a balanced tree as new node is added to the right always
         );
      }

      public static int Count<T>(this Tree<T> tree)
      {
         return tree.Match
         (
            Leaf: _ => 1,
            Branch: (left, right) => left.Count() + right.Count()
         );
      }

      public static Tree<R> Map<T, R>(this Tree<T> tree, Func<T, R> f)
      {
         return tree.Match
         (
            Leaf: t => Leaf(f(t)),
            Branch: (left, right) => Branch
            (
               Left: left.Map(f),
               Right: right.Map(f)
            )
         );
      }
   }
}