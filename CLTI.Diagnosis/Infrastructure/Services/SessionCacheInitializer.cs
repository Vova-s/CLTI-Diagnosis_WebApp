using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CLTI.Diagnosis.Infrastructure.Services;

/// <summary>
/// Automatically creates SessionCache table if it doesn't exist
/// </summary>
public class SessionCacheInitializer
{
    private readonly IOptions<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions> _options;
    private readonly ILogger<SessionCacheInitializer> _logger;
    private static bool _tableCreated = false;
    private static readonly object _lock = new object();

    public SessionCacheInitializer(
        IOptions<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions> options,
        ILogger<SessionCacheInitializer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public void EnsureCreated()
    {
        // Thread-safe check - create table only once
        if (_tableCreated)
            return;

        lock (_lock)
        {
            if (_tableCreated)
                return;

            try
            {
                using var connection = new SqlConnection(_options.Value.ConnectionString);
                connection.Open();

                // Check if table exists
                var tableExistsQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = @TableName AND schema_id = SCHEMA_ID(@SchemaName)";

                using var checkCmd = new SqlCommand(tableExistsQuery, connection);
                checkCmd.Parameters.AddWithValue("@TableName", _options.Value.TableName ?? "SessionCache");
                checkCmd.Parameters.AddWithValue("@SchemaName", _options.Value.SchemaName ?? "dbo");

                var exists = (int)checkCmd.ExecuteScalar() > 0;

                if (!exists)
                {
                    _logger.LogInformation("Creating SessionCache table...");

                    var createTableSql = $@"
                        CREATE TABLE [{_options.Value.SchemaName ?? "dbo"}].[{_options.Value.TableName ?? "SessionCache"}] (
                            [Id] NVARCHAR(449) NOT NULL,
                            [Value] VARBINARY(MAX) NOT NULL,
                            [ExpiresAtTime] DATETIME2 NOT NULL,
                            [SlidingExpirationInSeconds] BIGINT NULL,
                            [AbsoluteExpiration] DATETIME2 NULL,
                            CONSTRAINT [PK_{_options.Value.TableName ?? "SessionCache"}] PRIMARY KEY CLUSTERED ([Id] ASC)
                        );

                        CREATE NONCLUSTERED INDEX [IX_{_options.Value.TableName ?? "SessionCache"}_ExpiresAtTime]
                            ON [{_options.Value.SchemaName ?? "dbo"}].[{_options.Value.TableName ?? "SessionCache"}]([ExpiresAtTime] ASC);";

                    using var createCmd = new SqlCommand(createTableSql, connection);
                    createCmd.ExecuteNonQuery();

                    _logger.LogInformation("✅ SessionCache table created successfully");
                }
                else
                {
                    _logger.LogDebug("SessionCache table already exists");
                }

                _tableCreated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating SessionCache table: {Error}", ex.Message);
                throw;
            }
        }
    }
}

