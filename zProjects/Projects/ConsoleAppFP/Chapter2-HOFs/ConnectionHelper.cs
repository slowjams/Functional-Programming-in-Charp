using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;


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