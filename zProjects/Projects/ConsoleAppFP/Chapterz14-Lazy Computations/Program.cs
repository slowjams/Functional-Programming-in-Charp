using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using Boc.Domain.Events;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;
using System.Text.Json;

namespace ConsoleAppFP.Chapterz14_Lazy_Computations
{
    public delegate dynamic TestDynanic<T>(T s);

    public static class Program14
    {       
        public static void Main_14(string[] args)
        {
            //----------------------------------------------V
            
            var lazyGrandma = () => "grandma";
            var turnBlue = (string s) => $"blue {s}";
            var lazyGrandmaBlue = lazyGrandma.Map(turnBlue);
            
            lazyGrandmaBlue(); // => "blue grandma"
            //----------------------------------------------Ʌ

            //----------------------------------------------V
           
            CreateUri("http://github.com").Run(); // => Success(http://github.com/)
            
            CreateUri("rubbish").Run();           // => Exception(Invalid URI: The format of the URI could not be...)

            Try(() => new Uri("http://google.com")).Run();  // you don't want to create a dedicate CreateUri function, do you?

            //----------------------------------------------Ʌ

            Try(() => ExtractUri_Bad("http://google.com")).Run();   // use Try to rescue unsafe functions

            //----------------------------------------------V



            //----------------------------------------------Ʌ

            TestDynanic<string> _ = (string s) => "Hello";

            TestDynanic<string> __ = (string s) => 3;
        }

        public delegate Exceptional<T> Try_<T>();  // <-------------------------

        public static Exceptional<T> Run<T>(this Try<T> f)  // extension method
        {
            try
            {
                return f();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        #region unsafe ExtractUri method that can be separated into two methods: CreateUri and Parse
        private static Uri ExtractUri_Bad(string json)  // unsafe method
        {
            var website = JsonSerializer.Deserialize<Website>(json);
            return new Uri(website.Uri);
        }
        #endregion

        public static Try<Uri> CreateUri(string uri)  // method is defined to return Try<T>
        {
            return () => new Uri(uri);
        }

        public static Try<T> Parse<T>(string s)  // method is defined to return Try<T>
        {
            return () => JsonSerializer.Deserialize<T>(s);
        }

        public static Try<Uri> ExtractUri_Bind(string json)  // not very readable, use the Linq version below
        {
            return Parse<Website>(json)  // Try<Website>
                   .Bind(website => CreateUri(website.Uri));
        }

        public static Try<Uri> ExtractUri(string json)
        {
            return
                from website in Parse<Website>(json)
                from uri in CreateUri(website.Uri)
                select uri;
        }

        #region
        //public static Exceptional<Uri> CreateUri(string uri)  // bad, as you need do write try catch for every methods
        //{
        //    try { return new Uri(uri); }
        //    catch (Exception ex) { return ex; }
        //}  

        public record Website(string Name, string Uri);
        #endregion
    }

    public delegate Exceptional<T> Try__<T>();

    public static partial class F
    {
        public static Try<T> Try_F<T>(Func<T> f) => () => f();
    }

    public static class TryExt
    {
        public static Try<R> Map<T, R>(this Try<T> @try, Func<T, R> f)
        {
            return () => @try.Run().Match<Exceptional<R>>(ex => ex, t => f(t));
        }

        public static Try<R> Bind<T, R>(this Try<T> @try, Func<T, Try<R>> f)
        {
            return () => @try.Run().Match<Exceptional<R>>(ex => ex, t => f(t).Run());
        }

    }

    public static class OptionExt
    {
        //public static Func<R> Map<T, R>(this Func<T> f, Func<T, R> g)
        //{
        //    return () => g(f());
        //}
        
        public static T GetOrElse<T>(this Option<T> opt, T defaultValue)
        {
            return opt.Match
            (
               None: () => defaultValue,
               Some: (t) => t
            );
        }

        public static T GetOrElse<T>(this Option<T> opt, Func<T> fallback)
        {
            return opt.Match
            (
               None: () => fallback(),
               Some: (t) => t
            );
        }

        public static Option<T> OrElse<T>(this Option<T> opt, Func<Option<T>> fallback)
        {
            return opt.Match
            (
               None: fallback,
               Some: (_) => opt
            );
        }

        public static Option<T> OrElse_Bad<T>(this Option<T> left, Option<T> right)  // to see CachingRepository example why it is bad
        {
            return left.Match
            (
               None:() => right, 
               Some: (_) => left
            );
        }
    }
}