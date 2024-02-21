using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BOC.Models;
using Dapper;
using SlowJams.Functional;
using Unit = System.ValueTuple;

namespace BOC.Helpers
{
   using static ConnectionHelper;

   public static class ConnectionHelper
   {
      public static R Connect<R>(string connString, Func<IDbConnection, R> f)
      {
         using (var conn = new SqlConnection(connString))
         {
            conn.Open();
            return f(conn);
         }
      }
   }

   public static class ConnectionStringExt
   {
      public static Func<object, IEnumerable<T>> Retrieve<T>(this ConnectionString connString, SqlTemplate sql)
      {
         return param => Connect(connString,  conn => conn.Query<T>(sql, param));
      }

      public static Func<object, Exceptional<Unit>> TryExecute(this ConnectionString connString, SqlTemplate sql)   // "save" Dapper's Connect
      {
         return param =>
         {
            try
            {
               Connect(connString, conn => conn.Execute(sql, param));
            }
            catch(Exception ex) 
            {
               return ex;
            }
            return Unit.Create();
         };
      }
   }
}
