using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slowjams.Functional
{
   public static partial class F   // used by client to assit them create Option<T> in a more convenient way
   {
      public static Option<T> Some<T>([NotNull] T? value) => new Option<T>(value ?? throw new ArgumentNullException(nameof(value)));
      public static NoneType None => default;  // the reason why we need this static method and NoneType is to assist users so they don't need to do `default(Option<string>)`
   }

   public struct Option<T>
   {
      private readonly T? value;
      private readonly bool isSome;

      internal Option(T value)
      {
         this.value = value;
         this.isSome = true;
      }

      public IEnumerable<T> AsEnumerable()
      {
         if (isSome)
            yield return value!;
      }

      public static implicit operator Option<T>(NoneType _) => default;

      //public static implicit operator Option<T>(T value) => value is null ? None : new Option<T>(value);  // if value is null, there are two operation of "overloads" which includes this one and the one above, 

      public static implicit operator Option<T>(T value) => value is null ? default : new Option<T>(value);  // not sure why the author choose the above one, maybe just for demo purpose, since None is more meaningful than default

      public R Match<R>(Func<R> None, Func<T, R> Some)
      {
         return isSome ? Some(value!) : None();
      }
   }

   public struct NoneType { }
}