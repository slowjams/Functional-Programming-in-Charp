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
using System.Threading.Tasks;

namespace ConsoleAppFP.Tasks_Traversable
{
    public static class TasksTraversable 
    {
        public static Airline jetstar = default;
        public static Airline tiger = default;

        public static async void main_()
        {
            //------------------------------------------------------------------------------V bad, use Map
            IEnumerable<Airline> airlines = default!;
            string from = "Australia", to = "China";
            DateTime departure = DateTime.Now.AddDays(15);

            IEnumerable<Task<IEnumerable<Flight>>> flights = airlines.Map(a => a.Flights(from, to, departure));  // IEnumerable<Task<IEnumerable<Flight>>> is not what we want
            //------------------------------------------------------------------------------Ʌ

            //------------------------------------------------------------------------------V good approach
            Task<IEnumerable<IEnumerable<Flight>>> result = airlines.Traverse(a => a.Flights(from, to, departure));
            IEnumerable<IEnumerable<Flight>> resultFinal = await result;   // can write those this and above line into one statement, just for demo and comparision purpose
            IEnumerable<Flight> resultUnwrap = resultFinal.Flatten().OrderBy(f => f.Price);
            //------------------------------------------------------------------------------Ʌ

        }

        public static async Task<IEnumerable<Flight>> Search(IEnumerable<Airline> airlines, string origin, string dest, DateTime departure)
        {
            IEnumerable<IEnumerable<Flight>> flights = await airlines.Traverse(a => a.Flights(origin, dest, departure));

            return flights.Flatten().OrderBy(f => f.Price);
        }

        //            Task<IEnumerable<IEnumerable<Flight>>>, R is IEnumerable<Flight>, T is Airline
        public static Task<IEnumerable<R>> TraverseA<T, R>(this IEnumerable<T> ts, Func<T, Task<R>> f)  // by default use applicative TraverseA (parallel, hence faster)
        { 
            return ts.Aggregate
            (
                seed: Task.FromResult(Enumerable.Empty<R>()),   // itself and rs are Task<IEnumerable<IEnumerable<Flight>>>
                func: (rs, t) => Task.FromResult(Append<R>())   // t is Airline
                                     .Apply(rs)
                                     .Apply(f(t))
            );
        }

        public static Task<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> list, Func<T, Task<R>> func)
        {                                                                             // Airline -> Task<IEnumerable<Flight>>
            return TraverseA(list, func);
        }

        public static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()  // T is IEnumerable<Flight>
        {
            return (ts, t) => ts.Append(t);
        }
    }

    //------------------------------------------------------------------------>>
    public interface Airline
    {
        //Task<Flight> BestFare(string from, string to, DateTime on);
        Task<IEnumerable<Flight>> Flights(string from, string to, DateTime departure);
    }

    public class Flight 
    { 
        public decimal Price { get; set; } 
    }
    //------------------------------------------------------------------------<<
}