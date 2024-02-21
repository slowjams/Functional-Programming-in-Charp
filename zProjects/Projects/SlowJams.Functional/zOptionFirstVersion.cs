using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlowJams.FunctionalDraft
{
   public static class MyLib
   {
      /*
      public static string GreetUgly(Option<String> greetee)
      {
         return greetee switch
         {
            None<string> => "Sorry, who?",
            Some<string>(var name) => $"Hello, {name}"
         };
      }
      */

      public static readonly NoneType None = default;  // the reason why we define this static method and NoneType is to assist users so they don't need to type `new None<string>()` to represent "null"

      public static Option<T> Some<T>(T t) => new Some<T>(t);
      
      public static void Sample()
      {
         var greetingUgly = Greet(new None<string>());  // ugly 

         var greeting1 = Greet(None);

         var greeting2 = Some("John");

         var greeting3 = Greet("John");

         var empty = new NameValueCollection();

         //Option<string> green = empty["green"];
      }

      public static string Greet(Option<String> greetee)
      {
         return greetee.Match(
            None: () => "Sorry, who?",
            Some: (name) => $"Hello, {name}"
         );
      }

      public static R Match<T, R>(this Option<T> opt, Func<R> None, Func<T, R> Some)
      {
         return opt switch
         {
            None<T> => None(),
            Some<T>(var t) => Some(t),
            _ => throw new ArgumentException("Option must be None or Some")
         };
      }
   }

   public abstract record Option<T> 
   { 
      public static implicit operator Option<T>(NoneType _) => new None<T>();

      public static implicit operator Option<T>(T value) => value is null ? new None<T>() : new Some<T>(value);
   }

   public record None<T> : Option<T>;

   public record Some<T> : Option<T>
   {
      private T Value { get; }

      public Some(T value) => Value = value ?? throw new ArgumentNullException();

      public void Deconstruct(out T value) => value = Value;
   }

   public struct NoneType { }

}
