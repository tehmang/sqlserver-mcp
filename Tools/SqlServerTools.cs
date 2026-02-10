using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public class SqlServerTools
{
    [McpServerTool(Name = "query"), Description("Execute a SQL query against a SQL Server database and return the results (limited to 100 rows by default)")]
    public static async Task<string> ExecuteQuery(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("The SQL query to execute")] string query,
        [Description("Maximum number of rows to return (default: 100, max: 1000)")] int? maxRows = 100,
        [Description("Optional timeout in seconds (default: 30)")] int? timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Limit max rows to prevent overwhelming responses
            var rowLimit = Math.Min(maxRows ?? 100, 1000);
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection)
            {
                CommandTimeout = timeoutSeconds ?? 30
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var results = new List<Dictionary<string, object?>>();
            var rowCount = 0;
            
            while (await reader.ReadAsync(cancellationToken) && rowCount < rowLimit)
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value is DBNull ? null : value;
                }
                results.Add(row);
                rowCount++;
            }
            
            var hasMore = await reader.ReadAsync(cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                rowCount = results.Count,
                data = results,
                truncated = hasMore,
                message = hasMore ? $"Results limited to {rowLimit} rows. Use WHERE or TOP clause to filter results." : null
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "list_tables"), Description("List all tables in a SQL Server database")]
    public static async Task<string> ListTables(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("Optional schema name to filter (default: all schemas)")] string? schema = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    TABLE_SCHEMA,
                    TABLE_NAME,
                    TABLE_TYPE
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'";

            if (!string.IsNullOrWhiteSpace(schema))
            {
                query += " AND TABLE_SCHEMA = @Schema";
            }

            query += " ORDER BY TABLE_SCHEMA, TABLE_NAME";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(schema))
            {
                command.Parameters.AddWithValue("@Schema", schema);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var tables = new List<object>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new
                {
                    schema = reader.GetString(0),
                    name = reader.GetString(1),
                    type = reader.GetString(2)
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                tableCount = tables.Count,
                tables
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "describe_table"), Description("Get detailed information about a table's structure including columns, data types, and constraints")]
    public static async Task<string> DescribeTable(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("The schema name")] string schema,
        [Description("The table name")] string tableName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    CASE 
                        WHEN pk.COLUMN_NAME IS NOT NULL THEN 'YES'
                        ELSE 'NO'
                    END AS IS_PRIMARY_KEY
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                        AND tc.TABLE_NAME = ku.TABLE_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                        AND tc.TABLE_SCHEMA = @Schema
                        AND tc.TABLE_NAME = @TableName
                ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
                WHERE c.TABLE_SCHEMA = @Schema
                    AND c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Schema", schema);
            command.Parameters.AddWithValue("@TableName", tableName);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var columns = new List<object>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var dataType = reader.GetString(1);
                if (!reader.IsDBNull(2) && reader.GetInt32(2) > 0)
                {
                    dataType += $"({reader.GetInt32(2)})";
                }
                else if (!reader.IsDBNull(3) && !reader.IsDBNull(4))
                {
                    dataType += $"({reader.GetByte(3)},{reader.GetInt32(4)})";
                }

                columns.Add(new
                {
                    name = reader.GetString(0),
                    dataType,
                    nullable = reader.GetString(5) == "YES",
                    defaultValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                    isPrimaryKey = reader.GetString(7) == "YES"
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                schema,
                tableName,
                columnCount = columns.Count,
                columns
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "list_databases"), Description("List all databases on the SQL Server instance")]
    public static async Task<string> ListDatabases(
        [Description("The SQL Server connection string (connect to master or any database)")] string connectionString,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    name,
                    database_id,
                    create_date,
                    state_desc,
                    recovery_model_desc
                FROM sys.databases
                WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')
                ORDER BY name";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var databases = new List<object>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(new
                {
                    name = reader.GetString(0),
                    databaseId = reader.GetInt32(1),
                    createDate = reader.GetDateTime(2),
                    state = reader.GetString(3),
                    recoveryModel = reader.GetString(4)
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                databaseCount = databases.Count,
                databases
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "execute_non_query"), Description("Execute a non-query SQL command (INSERT, UPDATE, DELETE, CREATE, etc.) and return the number of affected rows")]
    public static async Task<string> ExecuteNonQuery(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("The SQL command to execute")] string command,
        [Description("Optional timeout in seconds (default: 30)")] int? timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var sqlCommand = new SqlCommand(command, connection)
            {
                CommandTimeout = timeoutSeconds ?? 30
            };

            var affectedRows = await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                affectedRows,
                message = $"Command executed successfully. {affectedRows} row(s) affected."
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "list_stored_procedures"), Description("List all stored procedures in a SQL Server database")]
    public static async Task<string> ListStoredProcedures(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("Optional schema name to filter (default: all schemas)")] string? schema = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    ROUTINE_SCHEMA,
                    ROUTINE_NAME,
                    CREATED,
                    LAST_ALTERED
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_TYPE = 'PROCEDURE'";

            if (!string.IsNullOrWhiteSpace(schema))
            {
                query += " AND ROUTINE_SCHEMA = @Schema";
            }

            query += " ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(schema))
            {
                command.Parameters.AddWithValue("@Schema", schema);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var procedures = new List<object>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                procedures.Add(new
                {
                    schema = reader.GetString(0),
                    name = reader.GetString(1),
                    created = reader.GetDateTime(2),
                    lastAltered = reader.GetDateTime(3)
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                procedureCount = procedures.Count,
                procedures
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "list_functions"), Description("List all user-defined functions in a SQL Server database")]
    public static async Task<string> ListFunctions(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("Optional schema name to filter (default: all schemas)")] string? schema = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    ROUTINE_SCHEMA,
                    ROUTINE_NAME,
                    DATA_TYPE,
                    CREATED,
                    LAST_ALTERED
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_TYPE = 'FUNCTION'";

            if (!string.IsNullOrWhiteSpace(schema))
            {
                query += " AND ROUTINE_SCHEMA = @Schema";
            }

            query += " ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(schema))
            {
                command.Parameters.AddWithValue("@Schema", schema);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var functions = new List<object>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                functions.Add(new
                {
                    schema = reader.GetString(0),
                    name = reader.GetString(1),
                    returnType = reader.IsDBNull(2) ? "TABLE" : reader.GetString(2),
                    created = reader.GetDateTime(3),
                    lastAltered = reader.GetDateTime(4)
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                functionCount = functions.Count,
                functions
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool(Name = "get_routine_definition"), Description("Get the source code/definition of a stored procedure or function")]
    public static async Task<string> GetRoutineDefinition(
        [Description("The SQL Server connection string")] string connectionString,
        [Description("The schema name")] string schema,
        [Description("The routine name (stored procedure or function)")] string routineName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    r.ROUTINE_TYPE,
                    r.DATA_TYPE,
                    r.CREATED,
                    r.LAST_ALTERED,
                    m.definition
                FROM INFORMATION_SCHEMA.ROUTINES r
                INNER JOIN sys.sql_modules m
                    ON OBJECT_ID(r.ROUTINE_SCHEMA + '.' + r.ROUTINE_NAME) = m.object_id
                WHERE r.ROUTINE_SCHEMA = @Schema
                    AND r.ROUTINE_NAME = @RoutineName";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Schema", schema);
            command.Parameters.AddWithValue("@RoutineName", routineName);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (await reader.ReadAsync(cancellationToken))
            {
                var routineType = reader.GetString(0);
                var returnType = reader.IsDBNull(1) ? "TABLE" : reader.GetString(1);
                var created = reader.GetDateTime(2);
                var lastAltered = reader.GetDateTime(3);
                var definition = reader.GetString(4);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    schema,
                    name = routineName,
                    type = routineType,
                    returnType = routineType == "FUNCTION" ? returnType : null,
                    created,
                    lastAltered,
                    definition
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Routine '{schema}.{routineName}' not found",
                    type = "NotFound"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
