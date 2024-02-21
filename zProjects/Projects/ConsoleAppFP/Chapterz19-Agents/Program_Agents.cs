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

namespace ConsoleAppFP.Chapterz19_Agents
{
    public static class Program19
    {      
        public static void Main_(string[] args)
        {
            Agent<string> logger = Agent.Start((string msg) => Console.WriteLine(msg));
            
            logger.Tell("Agent X");

            Agent<string> ping, pong = null;

            ping = Agent.Start((string msg) =>
            {
                if (msg == "STOP")
                    return;

                logger.Tell($"Received '{msg}'; Sending 'Ping'");
                Task.Delay(1000).Wait();
                pong.Tell("PING");
            });

            pong = Agent.Start(0, (int count, string msg) =>
            {
                int newCount = count + 1;
                string nextMsg = (newCount < 5) ? "PONG" : "STOP";

                logger.Tell($"Received '{msg}' #{newCount}; Sending '{nextMsg}'");
                Task.Delay(500).Wait();
                ping.Tell(nextMsg);

                return newCount;
            });

            ping.Tell("START");

            Console.ReadLine();
        }      
    }

    public static class Agent
    {
        public static Agent<Msg> Start<State, Msg>(State initialState, Func<State, Msg, State> process)
        {
            return new StatefulAgent<State, Msg>(initialState, process);
        }

        public static Agent<Msg> Start<State, Msg>(State initialState, Func<State, Msg, Task<State>> process)
        {
            return new StatefulAgent<State, Msg>(initialState, process);
        }

        public static Agent<Msg> Start<Msg>(Action<Msg> action)
        {
            return new StatelessAgent<Msg>(action);
        }
    }

    public interface Agent<Msg>
    {
        void Tell(Msg message);
    }

    //------------------------------------V
    public class StatefulAgent<State, Msg> : Agent<Msg>
    {
        private State state;
        private readonly ActionBlock<Msg> actionBlock;

        public StatefulAgent(State initialState, Func<State, Msg, State> process)
        {
            state = initialState;

            actionBlock = new ActionBlock<Msg>(msg =>
            {
                State newState = process(state, msg);
                state = newState;
            });
        }

        public StatefulAgent(State initialState, Func<State, Msg, Task<State>> process)
        {
            state = initialState;

            actionBlock = new ActionBlock<Msg>(async msg => state = await process(state, msg));
        }

        public void Tell(Msg message) => actionBlock.Post(message);
    }
    //------------------------------------Ʌ

    //-------------------------------------------V
    public class StatelessAgent<Msg> : Agent<Msg>
    {
        private readonly ActionBlock<Msg> actionBlock;

        public StatelessAgent(Action<Msg> process)
        {
            actionBlock = new ActionBlock<Msg>(process);
        }

        public StatelessAgent(Func<Msg, Task> process)
        {
            actionBlock = new ActionBlock<Msg>(process);
        }

        public void Tell(Msg message) => actionBlock.Post(message);
    }
    //-------------------------------------------Ʌ

    #region a simple implementation of Agent
    //public sealed class Agent<State, Msg>
    //{
    //    BlockingCollection<Msg> inbox = new BlockingCollection<Msg>(new ConcurrentQueue<Msg>());

    //    public void Tell(Msg message) => inbox.Add(message);

    //    public Agent(State initialState, Func<State, Msg, State> process)  // Constructor
    //    {
    //        void Loop(State state)
    //        {
    //            Msg message = inbox.Take();
    //            State newState = process(state, message);
    //            Loop(newState);
    //        }

    //        Task.Run(() => Loop(initialState));
    //    }
    //}
    #endregion
}











