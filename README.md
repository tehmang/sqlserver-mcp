# SQL Server MCP Server

A Model Context Protocol (MCP) server for SQL Server built with .NET 10. This server provides tools to interact with SQL Server databases through the MCP protocol.

**✅ Compatible with SQL Server 2005 and later versions**

## Features

This MCP server exposes the following tools for SQL Server operations:

- **query** - Execute SQL queries and return results
- **list_tables** - List all tables in a database
- **describe_table** - Get detailed table structure information
- **list_databases** - List all databases on the server
- **list_stored_procedures** - List all stored procedures in a database
- **list_functions** - List all user-defined functions in a database
- **get_routine_definition** - Get the source code of a stored procedure or function
- **execute_non_query** - Execute INSERT, UPDATE, DELETE, CREATE commands

## Prerequisites

- .NET 10 SDK or later
- SQL Server instance (SQL Server 2005 or later)
- Valid SQL Server connection string

## Installation

1. Clone or download this repository
2. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### Configuration for Visual Studio 2026

This MCP server is designed to work with Visual Studio 2026's MCP integration. 

#### Running the Server Standalone

The server uses stdio transport for communication:

```bash
dotnet run
```

#### Configuring in Visual Studio 2026

In Visual Studio 2026, configure the MCP server through:

**Tools > Options > AI > Model Context Protocol**

Add a new MCP server with the following settings:
- **Name**: SQL Server
- **Type**: stdio
- **Command**: `dotnet`
- **Arguments**: `run --project c:\Dev\sqlserver-mcp\SqlServerMcp.csproj`
- **Working Directory**: `c:\Dev\sqlserver-mcp`

Alternatively, you can publish the project and use the executable:

```bash
dotnet publish -c Release -o publish
```

Then configure:
- **Command**: `c:\Dev\sqlserver-mcp\publish\SqlServerMcp.exe`
- **Arguments**: (leave empty)

### Connection Strings for SQL Server 2005

For **SQL Server 2005**, use these connection string formats:

**Windows Authentication (Recommended for SQL Server 2005):**
```
Data Source=VM-SQLTEST;Database=master;Integrated Security=True;Encrypt=False
```

Or using the standard format:
```
Server=localhost;Database=MyDatabase;Integrated Security=true;Encrypt=false;
```

**SQL Server Authentication:**
```
Data Source=VM-SQLTEST;Database=MyDatabase;User Id=sa;Password=YourPassword;Encrypt=False
```

**Remote Server with specific port:**
```
Server=192.168.1.100,1433;Database=MyDatabase;User Id=myuser;Password=mypass;Encrypt=false;
```

**Named Instance:**
```
Server=localhost\SQLEXPRESS;Database=MyDatabase;Integrated Security=true;Encrypt=false;
```

### Connection Strings for SQL Server 2008+

For **modern SQL Server versions**, use:

**Windows Authentication:**
```
Server=localhost;Database=MyDatabase;Integrated Security=true;TrustServerCertificate=true;
```

**SQL Server Authentication:**
```
Server=localhost;Database=MyDatabase;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```

### 🔒 Security Notes

- **SQL Server 2005**: This server enables TLS 1.0 support for compatibility. Use `Encrypt=false` in connection strings.
- **Production environments**: Consider upgrading to a modern SQL Server version with TLS 1.2+ support.
- **Certificates**: The server accepts self-signed certificates for development purposes.

## Available Tools

### query
Execute a SQL query and return results as JSON.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `query` (string, required): The SQL query to execute
- `maxRows` (int, optional): Maximum rows to return (default: 100, max: 1000)
- `timeoutSeconds` (int, optional): Query timeout in seconds (default: 30)

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;Encrypt=false;",
  "query": "SELECT TOP 10 * FROM Sales.Customer",
  "timeoutSeconds": 60
}
```

### list_tables
List all tables in a database.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `schema` (string, optional): Filter by schema name

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;Encrypt=false;",
  "schema": "Sales"
}
```

### describe_table
Get detailed information about a table's structure.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `schema` (string, required): Schema name
- `tableName` (string, required): Table name

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;Encrypt=false;",
  "schema": "Sales",
  "tableName": "Customer"
}
```

### list_databases
List all user databases on the SQL Server instance (auto-detects SQL Server 2005).

**Parameters:**
- `connectionString` (string, required): SQL Server connection string (can connect to any database)

**Example:**
```json
{
  "connectionString": "Server=localhost;Integrated Security=true;Encrypt=false;"
}
```

### list_stored_procedures
List all stored procedures in a database.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `schema` (string, optional): Filter by schema name

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true;",
  "schema": "dbo"
}
```

### list_functions
List all user-defined functions in a database.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `schema` (string, optional): Filter by schema name

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true;",
  "schema": "dbo"
}
```

### get_routine_definition
Get the source code/definition of a stored procedure or function.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `schema` (string, required): Schema name
- `routineName` (string, required): Name of the stored procedure or function

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true;",
  "schema": "dbo",
  "routineName": "uspGetEmployeeManagers"
}
```

**Response includes:**
- Routine type (PROCEDURE or FUNCTION)
- Return type (for functions)
- Creation and last altered dates
- Complete source code definition

### execute_non_query
Execute a non-query SQL command (INSERT, UPDATE, DELETE, CREATE, etc.).

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `command` (string, required): The SQL command to execute
- `timeoutSeconds` (int, optional): Command timeout in seconds (default: 30)

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=MyDatabase;Integrated Security=true;TrustServerCertificate=true;",
  "command": "INSERT INTO Users (Name, Email) VALUES ('John Doe', 'john@example.com')"
}
```

## Response Format

All tools return JSON responses with a consistent format:

**Success Response:**
```json
{
  "success": true,
  "data": [...],
  "rowCount": 5
}
```

**Error Response:**
```json
{
  "success": false,
  "error": "Error message",
  "type": "SqlException"
}
```

## Security Considerations

- Always use secure connection strings
- Consider using Windows Authentication when possible
- Never hardcode passwords in your code
- Use environment variables or secure configuration for connection strings
- Be cautious with execute_non_query as it can modify data
- Limit permissions on SQL Server accounts used by the MCP server
- **SQL Server 2005**: Be aware that TLS 1.0 is enabled for compatibility but is considered insecure for production use

## SQL Server Version Compatibility

| Version | Status | Notes |
|---------|--------|-------|
| SQL Server 2005 | ✅ Fully Supported | Use `Encrypt=false` in connection strings |
| SQL Server 2008/2008 R2 | ✅ Fully Supported | Use `TrustServerCertificate=true` |
| SQL Server 2012+ | ✅ Fully Supported | Use `TrustServerCertificate=true` |
| SQL Server 2016+ | ✅ Fully Supported | Use `TrustServerCertificate=true` |
| SQL Server 2019+ | ✅ Fully Supported | Use `TrustServerCertificate=true` |
| SQL Server 2022 | ✅ Fully Supported | Use `TrustServerCertificate=true` |

The server automatically detects SQL Server 2005 and adapts its queries for compatibility.

## Development

### Project Structure
```
sqlserver-mcp/
├── Program.cs              # Main entry point
├── Tools/
│   └── SqlServerTools.cs  # MCP tool implementations
├── SqlServerMcp.csproj    # Project file
├── .github/
│   └── copilot-instructions.md
└── README.md              # This file
```

### Adding New Tools

To add a new tool:

1. Add a new method to `SqlServerTools.cs` with the `[McpServerTool]` attribute
2. Add a `[Description]` attribute for documentation
3. Use descriptive parameter names with `[Description]` attributes
4. Return JSON formatted results

Example:
```csharp
[McpServerTool(Name = "my_tool"), Description("Description of what the tool does")]
public static async Task<string> MyTool(
    [Description("Parameter description")] string parameter,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

## Debugging in Visual Studio 2026

You can debug this MCP server directly in Visual Studio 2026:

1. Open the solution in Visual Studio 2026
2. Set breakpoints in your code
3. Press F5 to start debugging
4. Visual Studio will automatically launch the MCP server
5. Use GitHub Copilot or other AI features in VS 2026 that will connect to your server
6. Breakpoints will be hit when the AI invokes your SQL Server tools

## License

This project is provided as-is for educational and development purposes.

## Resources

- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/)
