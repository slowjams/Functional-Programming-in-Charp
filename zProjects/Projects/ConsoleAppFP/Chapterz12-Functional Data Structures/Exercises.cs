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

   public static class Exercises
   {
      public static void Main_Exercises(string[] args)
      {

      }

      public class LabelTree<T>
      {
         public T Label { get; }
         public List<LabelTree<T>> Children { get; }

         public LabelTree(T label, List<LabelTree<T>> children)
         {
            Label = label;
            Children = children;
         }

         public override string ToString() => $"{Label}: {Children}";
         public override bool Equals(object other) => this.ToString() == other.ToString();
      }

      public static List<T> InsertAt<T>(this List<T> @this, int index, T value)
      {
         if (index == 0)
         {
            return List(value, @this);
         }
         else
         {
            return @this.Match(() => throw new IndexOutOfRangeException(), (t, ts) => List(t, ts.InsertAt(index - 1, value)));
         }
      }

      public static List<T> RemoveAt<T>(this List<T> @this, int index)
      {
         return @this.Match
         (
            () => throw new IndexOutOfRangeException(),
            (t, ts) => index == 0 ? ts : List(t, ts.RemoveAt(index - 1))  // you can take the figure 12.2 as example and remove pineapple item then you will see
         );
      }

      public static List<T> TakeWhile<T>(this List<T> @this, Func<T, bool> pred)
      {
         return @this.Match
         (
            () => List<T>(),  // () => new Empty<T>() for readability
            (t, ts) => pred(t) ? List(t, ts.TakeWhile(pred)) : List<T>()
         );                                               // : new Empty<T>()
      }

      public static List<T> DropWhile<T>(this List<T> @this, Func<T, bool> pred)
      {
         return @this.Match
         (
            Empty: () => @this,
            Cons: (head, tail) => pred(head) ? tail.DropWhile(pred) : @this  // no List fucntion involved, i.e doesn't need to creat new nodes like TakeWhile above                                                                 
         );
      }

      public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> @this, Func<T, bool> pred)
      {
         foreach (var item in @this)
         {
            if (pred(item)) 
               yield return item;
            else 
               yield break;
         }
      }

      public static IEnumerable<T> DropWhile<T>(this IEnumerable<T> @this, Func<T, bool> pred)
      {
         bool clean = true;
         foreach (var item in @this)
         {
            if (!clean || !pred(item))
            {
               yield return item;
               clean = false;
            }
         }
      }

      public static Tree<R> Bind<T, R>(this Tree<T> tree, Func<T, Tree<R>> f)
      {
         return tree.Match
         (
            Leaf: f,
            Branch: (l, r) => new Branch<R>(l.Bind(f), r.Bind(f))
         );
      }
   }
}