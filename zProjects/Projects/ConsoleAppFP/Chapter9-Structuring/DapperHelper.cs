using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using static ConnectionHelper;
//using SlowJams.Functional;
//using static SlowJams.Functional.F;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace ConsoleAppFP.Chapter9_Structuring
{
   public class Program9_DapperHelper
   {
      public static void Main9(string[] args)
      {
         Option<string> aaa = null;
      }
   }

   public static class ConnectionStringExtensions
   {
      public static Func<object, IEnumerable<T>> Retrieve<T>(this ConnectionString connStr, SqlTemplate sql)
      {
         return param => Connect(connStr, conn => conn.Query<T>(sql, param));
      }
   }

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
}
