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

namespace ConsoleAppFP.Chapterz15_StatefulProgramz
{
    public class Program15_SecondDesign
    {
        public static void Main15_()
        {
            MainRec("Enter a currency pair like 'EURUSD', or 'q' to quit", Rates.Empty);

            Console.ReadLine();
        }
        static void MainRec(string message, Rates cache)
        {
            Console.WriteLine(message);

            var input = Console.ReadLine().ToUpper();

            if (input == "Q") 
                return;

            GetRate(RatesApi.TryGetRate, input, cache).Run().Match
            (
               ex => MainRec($"Error: {ex.Message}", cache),
               result => MainRec(result.Rate.ToString(), result.NewState)
            );

        }

        static Try<(decimal Rate, Rates NewState)> GetRate(Func<string, Try<decimal>> getRate, string ccyPair, Rates cache)
        {
            if (cache.ContainsKey(ccyPair))
                return Try(() => (cache[ccyPair], cache));
            else
                return 
                    from rate in getRate(ccyPair)
                    select (rate, cache.Add(ccyPair, rate));

            //var rate = RatesApi.GetRate(ccyPair);            
        }
    }

    public static class RatesApi
    {
        public static Try<decimal> TryGetRate(string ccyPair) => () => GetRate(ccyPair);

        public static decimal GetRate(string ccyPair)
        {
            Console.WriteLine($"fetching rate...");
            // ... Perform Http web request
            return 3;
        }
    }
}



