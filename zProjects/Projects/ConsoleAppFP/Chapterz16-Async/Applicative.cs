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
using System.Threading.Tasks;
using System.Net.Http;

namespace ConsoleAppFP.Model
{
    public static class Tasks_Traversable 
    {
        public static Airline jetstar = default;
        public static Airline tiger = default;

        public static void main_()
        {

        }

        //-----------------------------------------------------------------------------------------------------V
        public static Task<Flight> BestFareSequential_NotSoGood(string origin, string dest, DateTime departure)
        {
            return
                from j in jetstar.BestFare(origin, dest, departure)
                from t in tiger.BestFare(origin, dest, departure)
                select j.Price < t.Price ? j : t;
        }

        public static async Task<RR> SelectMany_BestFareSequential_NotSoGood<T, R, RR>(this Task<T> task, Func<T, Task<R>> bind, Func<T, R, RR> project)
        {
            T t = await task;
            R r = await bind(t);  // second task only starts when first task is completed
            return project(t, r);
        }
        //-----------------------------------------------------------------------------------------------------Ʌ

        //----------------------------------------------------------------------------------------------V  those method belows need to be modified when copying to walkthrough
        public static Task<Flight> BestFareParallel_Good(string origin, string dest, DateTime departure)
        {          
            Func<Flight, Flight, Flight> pickCheaper = (Flight l, Flight r) => l.Price < r.Price ? l : r;

            return
                Async(pickCheaper)
                .Apply(jetstar.BestFare(origin, dest, departure)) // first task can immediately returns
                .Apply(tiger.BestFare(origin, dest, departure));  // then second task starts to execute, so it is parallel
        }

        public static async Task<R> Apply_<T, R>(this Task<Func<T, R>> f, Task<T> arg)
        {
            return (await f)(await arg);
        }

        public static Task<Func<T2, R>> Apply__<T1, T2, R>(this Task<Func<T1, T2, R>> f, Task<T1> arg)
        {
            return Apply_(f.Map(F.Curry), arg);
        }

        public static async Task<R> Map_<T, R>(this Task<T> task, Func<T, R> f)
        {
            //=> f(await task);
            return f(await task.ConfigureAwait(false));
        }

        public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> func)
        {
            return t1 => t2 => func(t1, t2);
        }
        //----------------------------------------------------------------------------------------------Ʌ
    }

    //------------------------------------------------------------------------>>
    public interface Airline
    {
        Task<Flight> BestFare(string from, string to, DateTime on);
        Task<IEnumerable<Flight>> Flights(string from, string to, DateTime on);
    }

    public class Flight 
    { 
        public decimal Price { get; set; } 
    }
    //------------------------------------------------------------------------<<
}