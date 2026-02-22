using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SqlServerMcp.Tools;
using System.Net;
using System.Net.Security;

// Configuration pour SQL Server 2005 : Activer TLS 1.0 et désactiver la validation stricte SSL
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |      // TLS 1.0 pour SQL Server 2005
                                       SecurityProtocolType.Tls11 |    // TLS 1.1
                                       SecurityProtocolType.Tls12 |    // TLS 1.2
                                       SecurityProtocolType.Tls13;     // TLS 1.3

// Accepter tous les certificats SSL (nécessaire pour les anciens serveurs)
ServicePointManager.ServerCertificateValidationCallback = 
    (sender, certificate, chain, sslPolicyErrors) => true;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SqlServerTools>();

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

await builder.Build().RunAsync();
