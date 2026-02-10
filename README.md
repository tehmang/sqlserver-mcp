# SQL Server MCP Server

A Model Context Protocol (MCP) server for SQL Server built with .NET 10. This server provides tools to interact with SQL Server databases through the MCP protocol.

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
- SQL Server instance (local or remote)
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

### Using from MCP Client

Example connection strings:

**Windows Authentication:**
```
Server=localhost;Database=MyDatabase;Integrated Security=true;TrustServerCertificate=true;
```

**SQL Server Authentication:**
```
Server=localhost;Database=MyDatabase;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```

## Available Tools

### query
Execute a SQL query and return results as JSON.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string
- `query` (string, required): The SQL query to execute
- `timeoutSeconds` (int, optional): Query timeout in seconds (default: 30)

**Example:**
```json
{
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true;",
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
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true;",
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
  "connectionString": "Server=localhost;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true;",
  "schema": "Sales",
  "tableName": "Customer"
}
```

### list_databases
List all user databases on the SQL Server instance.

**Parameters:**
- `connectionString` (string, required): SQL Server connection string (can connect to any database)

**Example:**
```json
{
  "connectionString": "Server=localhost;Integrated Security=true;TrustServerCertificate=true;"
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
