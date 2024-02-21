using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Unit = System.ValueTuple;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Immutable;
using ConsoleAppFP.Chapterz18_Rx;

namespace ConsoleAppFP.Chapterz19_Agents
{
    using CcyAgents = ImmutableDictionary<string, Agent<string>>;  // if you refer to Chapter 15: Rates = System.Collections.Immutable.ImmutableDictionary<string, decimal>
                                                                   // so Rates is a cache, but if we had a different cache for every thread, then somehow combine different caches into a global cache
                                                                   // that would be suboptimal. That's why we use agents to make a single global cache
    public static class Program_CurrencyLookup
    {      
        public static void Main(string[] args)
        {
            Agent<FxRateResponse> sendResponse = Agent.Start((FxRateResponse res) => Console.WriteLine($"{res.CcyPair}: {res.Rate}"));

            Agent<FxRateRequest> rateLookup = StartReqProcessor(sendResponse);

            Parallel.ForEach(Range(1, 1000), i => rateLookup.Tell(new FxRateRequest(i % 2 == 0 ? "EURUSD" : "GBPUSD", i % 2 == 0 ? "ServiceA" : "ServiceB")));

            Task.Delay(10000).Wait();
        }     
        
        public static void Setup(MessageBroker broker)
        {
            Agent<FxRateResponse> sendResponse = Agent.Start((FxRateResponse res) => broker.Send(res.Recipient, res));

            Agent<FxRateRequest> processRequest = StartReqProcessor(sendResponse);

            // `processRequest.Tell` will be called on multiple threads by multiple services who post message on this channel
            broker.Subscribe<FxRateRequest>("FxRateRequests", processRequest.Tell);  // when a request is received, pass it to the processing agent
                                                                                     // here we go from multithreaded to sequential
        }

        // Agent<FxRateRequest> should not do long processing work, so it doesn't have any `await` and it just posts message to Agent<string> agent which uses `await`
        public static Agent<FxRateRequest> StartReqProcessor(Agent<FxRateResponse> sendResponse)  
        {                                                                                        
            return Agent.Start(CcyAgents.Empty, (CcyAgents state, FxRateRequest request) =>
            {
                string ccyPair = request.CcyPair;

                Agent<string> agent = state.Lookup(ccyPair).GetOrElse(() => StartAgentFor(ccyPair, sendResponse));

                agent.Tell(request.Sender);

                return state.Add(ccyPair, agent);
            });
        }

        public static Agent<string> StartAgentFor(string ccPair, Agent<FxRateResponse> sendResponse)
        {
            return Agent.Start<Option<decimal>, string>  // public class StatefulAgent<State, Msg> where State is Option<decimal>, Msg is string
            (
                initialState: None,
                process: async (optRate, recipient) =>
                {
                    decimal rate = await optRate.GetOrElse(() => RatesApi.GetRateAsync(ccPair));

                    sendResponse.Tell(new FxRateResponse
                    (
                        CcyPair: ccPair,
                        Rate: rate,   // <-------------this is what we want in response
                        Recipient: recipient
                    ));

                    return Some(rate);
                }
            );
        }
    }

    public record FxRateRequest(string CcyPair, string Sender);

    public record FxRateResponse(string CcyPair, decimal Rate, string Recipient);

    //------------------------V
    public class MessageBroker
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");

        public void Subscribe<T>(string channel, Action<T> act)
            => redis.GetSubscriber().Subscribe(channel, (_, val) => act(JsonConvert.DeserializeObject<T>(val)));

        public void Send(string channel, object message)
            => redis.GetDatabase(0).PublishAsync(channel, JsonConvert.SerializeObject(message));
    }
    //------------------------Ʌ
}








