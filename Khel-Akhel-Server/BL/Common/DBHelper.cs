using System.Data;
using Microsoft.Data.SqlClient;

namespace Khel_Akhel_Server.BL.Common
{
    public class DbHelper
    {
        private readonly IConfiguration _config;

        public DbHelper(IConfiguration config)
        {
            _config = config;
        }

        private static readonly string[] ForbiddenKeywords = new[]
        {
            "ALTER",
            "DROP",
            "CREATE",
            "BACKUP",
            "RESTORE",
        };

        public static void ValidateQuerySafety(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            foreach (var keyword in ForbiddenKeywords)
            {
                if (
                    System.Text.RegularExpressions.Regex.IsMatch(
                        query,
                        $@"\b{keyword}\b",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"Forbidden SQL keyword '{keyword}' detected. DDL/Backup commands are strictly prohibited in application queries."
                    );
                }
            }
        }

        private string GetConnectionString()
        {
            string? cs = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(cs))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured."
                );
            }
            return cs;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(GetConnectionString());
        }

        public DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            ValidateQuerySafety(query);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using SqlConnection con = GetConnection();
                using SqlCommand cmd = new SqlCommand(query, con);

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                sw.Stop();
                return dt;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Database ExecuteQuery failed", ex);
                throw;
            }
        }

        public int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            ValidateQuerySafety(query);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using SqlConnection con = GetConnection();
                con.Open();

                using SqlCommand cmd = new SqlCommand(query, con);

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                int result = cmd.ExecuteNonQuery();

                sw.Stop();
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Database ExecuteNonQuery failed", ex);
                throw;
            }
        }

        public object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
        {
            ValidateQuerySafety(query);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using SqlConnection con = GetConnection();
                con.Open();

                using SqlCommand cmd = new SqlCommand(query, con);

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                object? result = cmd.ExecuteScalar();

                sw.Stop();

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error("Database ExecuteScalar failed", ex);
                throw;
            }
        }

        public SqlConnection GetOpenConnection()
        {
            var con = new SqlConnection(GetConnectionString());
            con.Open();

            return con;
        }

        public DataTable ExecuteStoredProcedure(string spName, SqlParameter[]? parameters = null)
        {
            ValidateQuerySafety(spName);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using SqlConnection con = GetConnection();
                using SqlCommand cmd = new SqlCommand(spName, con);

                cmd.CommandType = CommandType.StoredProcedure;

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                sw.Stop();

                return dt;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logs.Error($"Stored Procedure failed | Procedure:{spName}", ex);
                throw;
            }
        }
    }
}
