using Serilog;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigRunner.Core
{
    public sealed class SqlRunner
    {
        private readonly SqlRunnerOptions _options;
        private readonly ILogger _logger;

        public SqlRunner(ILogger logger, SqlRunnerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Run(CancellationToken token)
        {
            var connectionString = _options.ConnectionString;
            var sqlFilePath = _options.SqlFilePath;
            var terminator = _options.Terminator;

            using (var sqlConnection = await InputConnectionStringData(connectionString, token).ConfigureAwait(false))
            using (var reader = GetSqlScriptReader(sqlFilePath, token))
            {
                var sqlCommand = default(SqlCommand);
                var builder = new StringBuilder();
                var scriptLine = string.Empty;
                var isFirstLine = true;

                var watch = Stopwatch.StartNew();

                while ((scriptLine = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(scriptLine))
                        {
                            if (isFirstLine)
                            {
                                builder = builder.Append(scriptLine);
                                isFirstLine = false;
                            }
                            else
                            {
                                builder = builder
                                            .Append(" ")
                                            .Append(scriptLine);
                            }
                        }

                        switch (terminator)
                        {
                            case BatchTerminator.GO:
                                if (scriptLine.EndsWith("GO"))
                                {
                                    // by doing this you move the pointer (i.e. last index) back one character but you don't change the mutability of the object.
                                    builder.Length--;
                                    builder.Length--;

                                    await ExcuteQuery(builder.ToString()).ConfigureAwait(false);
                                }
                                break;

                            case BatchTerminator.NewLine:
                                await ExcuteQuery(builder.ToString()).ConfigureAwait(false);
                                break;
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.Error(ex, "A sql command caused an error.");
                        builder = new StringBuilder();
                        isFirstLine = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Something went wrong and caused error.");
                        break;
                    }
                }

                Task<int> ExcuteQuery(string commandText)
                {
                    sqlCommand = sqlConnection.CreateCommand();
                    sqlCommand.CommandText = commandText;
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.CommandTimeout = 0;

                    return sqlCommand.ExecuteNonQueryAsync(token);
                }
            }
        }

        private async Task<SqlConnection> InputConnectionStringData(string connectionString, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var sqlConnection = new SqlConnection(connectionString);
                    await sqlConnection.OpenAsync(token).ConfigureAwait(false);

                    if (sqlConnection.State == ConnectionState.Open)
                    {
                        return sqlConnection;
                    }
                    else
                    {
                        Console.WriteLine($"[Error] Your database is in {sqlConnection.State.ToString()} status");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Establishing a database connection caused an error.");
                }
            }

            return null;
        }

        private TextReader GetSqlScriptReader(string sqlFilePath, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var reader = new StreamReader(sqlFilePath);
                    if (reader != null)
                    {
                        return reader;
                    }
                    else
                    {
                        _logger.Verbose("Opening the sql script failed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Opening the sql script file caused an error.");
                }
            }

            return null;
        }
    }
}
