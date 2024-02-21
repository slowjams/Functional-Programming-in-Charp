using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using Boc.Domain.Events;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using System.Data.SqlClient;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;
using Dapper;
using ConsoleAppFP.Chapter9_Structuring;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Data;

namespace ConsoleAppFP.Chapterz14_Lazy_Computationss
{
    public delegate R Middleware<T, R>(Func<T, R> continuation);

    public static class Middleware_Extz
    {
        public static Middleware<T, R> Bind<T, R>(this Middleware<T, R> mw, Func<T, Middleware<T, R>> f)
        {
            return continuation => mw(t => f(t)(continuation));
        }

        //public static Middleware<T, R> Select<T, R>
        //  (this Middleware<T, R> mw, Func<T, R> f)
        //  => cont => mw(t => cont(f(t)));

        public static Middleware<T, R> SelectMany<T, R>
           (this Middleware<T, R> mw, Func<T, Middleware<T, R>> f)
           => cont => mw(t => f(t)(cont));

        //public static Middleware<T, RR> SelectMany<T, R, RR>
        //   (this Middleware<T, R> @this, Func<T, Middleware<T, R>> f, Func<T, R, RR> project)
        //   => cont => @this(t => f(t)(r => cont(project(t, r))));

        public static Func<T> ToNullary<T>(this Func<Unit, T> f) => () => f(Unit());
    }


    public class DbLogger
    {
        private Middleware<SqlConnection, int> Connect;

        private Middleware<Unit, int> Time;

        //private Func<string, Middleware<Unit>> Trace;

        public DbLogger(ConnectionString connString, ILogger logger)
        {
            Connect = f => ConnectionHelper.Connect(connString, f);

            Time = f => Instrumentation.Time(logger, "operatonNmae", f.ToNullary());
        }

        //Middleware<SqlConnection, int> BasicPipeline =>
        //    from _ in Time
        //    from conn in Connect
        //    select conn;

        //public int Dynamic_Log(LogMessage message)
        //{
        //    return BasicPipeline(conn => conn.Execute("sp_create_log", message, commandType: CommandType.StoredProcedure));
        //}

        public void Log_1(LogMessage message)
        {
            Connect(conn => conn.Execute("sp_create_log", message, commandType: CommandType.StoredProcedure));
        }
    }

    //----------------------------------->>
    public static class ConnectionHelper
    {
        public static R Connect<R>
           (string connString, Func<SqlConnection, R> func)
        {
            using var conn = new SqlConnection(connString);
            conn.Open();
            return func(conn);
        }

        public static R Transact<R>
           (SqlConnection conn, Func<SqlTransaction, R> f)
        {
            using var tran = conn.BeginTransaction();

            R r = f(tran);
            tran.Commit();

            return r;
        }
    }
    //-----------------------------------<<

    //----------------------------------->>
    public static class Instrumentation
    {
        public static T Time<T>(ILogger logger, string op, Func<T> f)
        {
            var sw = new Stopwatch();

            sw.Start();
            T t = f();
            sw.Stop();      
            
            logger.LogDebug($"{op} took {sw.ElapsedMilliseconds}ms");
            
            return t;
        }

        public static T Trace<T>(ILogger logger, string op, Func<T> f)
        {
            logger.LogTrace($"Entering {op}");
            T t = f();
            logger.LogTrace($"Leaving {op}");
            return t;
        }
    }
    //-----------------------------------<<

    public static class TimeSpanExt
    {
        public static TimeSpan Seconds(this int @this)
           => TimeSpan.FromSeconds(@this);

        public static TimeSpan Minutes(this int @this)
           => TimeSpan.FromMinutes(@this);

        public static TimeSpan Days(this int @this)
           => TimeSpan.FromDays(@this);

        public static DateTime Ago(this TimeSpan @this)
           => DateTime.UtcNow - @this;
    }

    public class LogMessage { }
}