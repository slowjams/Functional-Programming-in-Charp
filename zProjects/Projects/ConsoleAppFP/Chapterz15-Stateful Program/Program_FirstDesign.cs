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
using Rates = System.Collections.Immutable.ImmutableDictionary<string, decimal>;

namespace ConsoleAppFP.Chapterz15_StatefulProgram
{
    public class Program15
    {
        public static void Main15()
        {
            //Console.WriteLine("Enter a currency pair like 'EURUSD', or 'q' to quit");
            //MainRec(Rates.Empty);

            Console.WriteLine("Enter a currency pair like 'EURUSD', or 'q' to quit");
            var state = Rates.Empty;
            for (string input; (input = Console.ReadLine().ToUpper()) != "Q";)
            {
                var (rate, newState) = GetRate(input, state);
                state = newState;
                Console.WriteLine(rate);
            }
        }
        static void MainRec(Rates cache)
        {
            var input = Console.ReadLine().ToUpper();
            if (input == "Q") return;
            var (rate, newState) = GetRate(input, cache);
            Console.WriteLine(rate);
            MainRec(newState);  // recursively calls itself with the new state
        }

        static (decimal, Rates) GetRate(string ccyPair, Rates cache)
        {
            if (cache.ContainsKey(ccyPair))
                return (cache[ccyPair], cache);

            var rate = RatesApi.GetRate(ccyPair);   // code smell, 
            return (rate, cache.Add(ccyPair, rate));
        }
    }

    public static class RatesApi
    {
        public static decimal GetRate(string ccyPair)
        {
            Console.WriteLine($"fetching rate...");
            // ... Perform Http web request
            return 3;
        }
    }
}



