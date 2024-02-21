using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;
using System.Threading.Tasks;
using System.Reactive.Subjects;


namespace ConsoleAppFP.Chapterz18_Rx
{  
    public static class Program18_KeyCombination
    {
        //public static void Main(string[] args)
        //{
        //    IEnumerable<string> e = new[] { "Hi", "There", "Bye" };
        //    IObservable<string> strings = e.ToObservable();

        //    var temp = strings.Select(_ => strings);
        //    //var temp = strings.SelectMany(_ => strings);

        //    temp.Trace("strings");
        //}

        //------------------------------------------------------------V
        public static void Main_Reacting_Mutiple_Events(string[] args)  // Reacting_Mutiple_Events
        {
            IObservable<decimal> balance = default!;
            IObservable<decimal> eurUsdRate = default!;

            var balanceInUsd = balance.CombineLatest(eurUsdRate, (bal, rate) => bal * rate);  // CombineLatest works but it produces too much data becuase exchange rate changes quite often :(

            //------------------------------------------V Use `Observable.Sample` to setup a "timer" to specify interval you want to singal
            TimeSpan tenMins = TimeSpan.FromMinutes(10);
            IObservable<long> sampler = Observable.Interval(tenMins);
            IObservable<decimal> eurUsdSampled = eurUsdRate.Sample(sampler);  // <---------you get the idea :)
            IObservable<decimal> balanceInUsdLessFrequent = balance.CombineLatest(eurUsdSampled, (bal, rate) => bal * rate);
            //------------------------------------------Ʌ

            //-----------------------------------------------V
            IObservable<Transaction> transactions = default!;
            decimal initialBalance = 0;
            IObservable<decimal> balanceAggregated = transactions.Scan(initialBalance, (bal, trans) => bal + trans.Amount);

            // you need this because you want to signal when the current balance is negative, and you don't want to signal again when previous balance is already negative
            IObservable<Unit> dipsIntoTheRed =  
                from bal in balanceAggregated.PairWithPrevious()
                where bal.Previous >= 0 && bal.Current < 0
                select Unit();
            //-----------------------------------------------Ʌ

            //------------------------------------------------V real world system needs to process transactions for all accounts, so we must group accountID
            IObservable<Transaction> transactions_ = default!;

            IObservable<Guid> dipsIntoRed = transactions
                .GroupBy(t => t.AccountId)  // IObservable<IGroupedObservable<Guid, Transaction>>
                .Select(DipsIntoTheRed)     // IObservable<IObservable<Guid>>
                .MergeAll();
            //------------------------------------------------Ʌ
        }

        //------------------------------------------------------------------------------------------------------->>
        public static IObservable<Guid> DipsIntoTheRed(IGroupedObservable<Guid, Transaction> transactions)
        {
            Guid accountId = transactions.Key;
            decimal initialBalance = 0;

            IObservable<decimal> balance = transactions.Scan(initialBalance, (bal, trans) => bal + trans.Amount);

            return
                from bal in balance.PairWithPrevious()
                where bal.Previous >= 0 && bal.Current < 0
                select accountId;
        }

        public static IObservable<T> MergeAll<T>(this IObservable<IObservable<T>> source)
            => source.SelectMany(x => x);
        //-------------------------------------------------------------------------------------------------------<<

        public static IObservable<(T Previous, T Current)> PairWithPrevious<T>(this IObservable<T> source)
        {
            return 
                from first in source
                from second in source.Take(1)
                select (Previous: first, Current: second);
        }

        public record Transaction(Guid AccountId, decimal Amount);
        //------------------------------------------------------------Ʌ

        public static void _Main(string[] args)
        {
            Console.WriteLine("Enter some inputs to push them to 'inputs', or 'q' to quit");

            var keys = new Subject<ConsoleKeyInfo>();
            var halfSec = TimeSpan.FromMilliseconds(500);

            IObservable<ConsoleKeyInfo> keysAlt = keys.Where(key => key.Modifiers.HasFlag(ConsoleModifiers.Alt));

            IObservable<(ConsoleKeyInfo First, ConsoleKeyInfo Second)> twoKeyCombis =
                from first  in keysAlt
                from second in keysAlt.Take(halfSec).Take(1)
                select (First: first, Second: second);

            IObservable<Unit> altKB =
                from tpl in twoKeyCombis
                where tpl.First.Key == ConsoleKey.K && tpl.Second.Key == ConsoleKey.B
                select Unit();

            using (keys.Select(k => Environment.NewLine + k.KeyChar).Trace("keys"))
            using (twoKeyCombis.Select(ks => $"{ks.Item1.KeyChar}-{ks.Item2.KeyChar}").Trace("twoKeyCombis"))
            using (altKB.Trace("altKB"))
                for (ConsoleKeyInfo key; (key = Console.ReadKey()).Key != ConsoleKey.Q;)
                    keys.OnNext(key);
        }

        public static void _Main_(string[] args)
        {
            var keys = new Subject<ConsoleKeyInfo>();

            IObservable<IObservable<ConsoleKeyInfo>> d = keys.Select(_ => keys);

            

        }

        public static void Mainxxx(string[] args)
        {
            //IEnumerable<string> e = new[] { "Hi", "There", "Bye" };
            //IObservable<string> strings = e.ToObservable();

            //IObservable<string> stringsTimed = strings.Take(TimeSpan.FromMilliseconds(1000));
            //stringsTimed.Trace("string");

            TimeSpan oneSec = TimeSpan.FromMilliseconds(100);

            IObservable<long> ticks = Observable.Interval(oneSec);

            IObservable<long> ticksTimed = ticks.Take(TimeSpan.FromMilliseconds(3000));

            ticksTimed.Trace("ticks");

            Console.ReadLine();
        }
    }
}







