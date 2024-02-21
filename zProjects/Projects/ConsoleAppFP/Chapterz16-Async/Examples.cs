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
    public static class CurrencyLayer  // let's say it's a paid subscriptionthat provides good quality exchange rate data
    {
        public static Task<decimal> GetRateAsync(string ccyPair) => Task.FromResult<decimal>(0);  // just for demo purpose
    }

    public static class RatesApi  // normal services to use when the above paid service stop working
    {
        public static Task<decimal> GetRateAsync(string ccyPair) => Task.FromResult<decimal>(0);  // just for demo purpose
    }
}