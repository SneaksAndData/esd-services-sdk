using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snd.Sdk.Storage.Models;
using Snd.Sdk.Hosting;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Cql;
using Snd.Sdk.Storage.Providers.Configurations;

namespace Snd.Sdk.Storage.Providers;

/// <summary>
/// Provider for any CQL-compatible NoSql service.
/// Use this in Startup to configure the app to run with Cql database.
/// </summary>
[ExcludeFromCodeCoverage]
public static class CqlServiceProvider
{
    /// <summary>
    /// Adds Cql session to the DI container.
    /// </summary>
    /// <param name="services">Service collection (DI container).</param>
    /// <param name="appConfiguration">Application configuration with "cql" section configured according to <see cref="CqlConfiguration"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddCqlStorage(this IServiceCollection services, IConfiguration appConfiguration)
    {
        services.AddSingleton(typeof(ISession), provider =>
        {
            var cqlConfiguration = new CqlConfiguration();
            appConfiguration.GetSection(nameof(CqlServiceProvider)).Bind(cqlConfiguration);

            var cluster = Cluster.Builder()
                .AddContactPoints(cqlConfiguration.ContactPoints)
                .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(cqlConfiguration.DataCenter))
                .WithAuthProvider(new PlainTextAuthProvider(cqlConfiguration.Username, cqlConfiguration.Password))
                .WithReconnectionPolicy(new ExponentialReconnectionPolicy(cqlConfiguration.ReconnectBaseDelayMs,
                    cqlConfiguration.ReconnectMaxDelayMs))
                .WithRetryPolicy(new LoggingRetryPolicy(new DefaultRetryPolicy()))
                .WithCompression(CompressionType.Snappy)
                .WithApplicationName(cqlConfiguration.ApplicationName)
                .WithApplicationVersion(Environment.GetEnvironmentVariable("APPLICATION_VERSION") ?? "0.0.0")
                .WithQueryTimeout(cqlConfiguration.QueryTimeout);

            if (cqlConfiguration.UseSsl)
            {
                cluster = cluster.WithSSL();
            }

            return cluster.Build().Connect(cqlConfiguration.KeySpace);
        });

        return services.AddSingleton<ICqlEntityService, CqlService>();
    }

    /// <summary>
    /// Adds Cql session to the DI container.
    /// </summary>
    /// <param name="services">Service collection (DI container).</param>
    /// <param name="appConfiguration">Application configuration with "cql" section configured according to <see cref="CqlConfiguration"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddAstraStorage(this IServiceCollection services, IConfiguration appConfiguration)
    {
        var cqlConfiguration = new CqlConfiguration();
        appConfiguration.GetSection(nameof(CqlServiceProvider)).Bind(cqlConfiguration);
        var savePath = Path.Combine(Path.GetTempPath(), ".astra");
        var clientId = EnvironmentExtensions.GetDomainEnvironmentVariable("ASTRA_CLIENT_ID");
        var clientSecret = EnvironmentExtensions.GetDomainEnvironmentVariable("ASTRA_CLIENT_SECRET");
        var bundleBytes = Convert.FromBase64String(EnvironmentExtensions.GetDomainEnvironmentVariable("ASTRA_BUNDLE"));

        using var fs = new FileStream(savePath, FileMode.Create, FileAccess.ReadWrite);
        fs.Write(bundleBytes);

        fs.Flush();
        fs.Close();

        var socketOptions = new SocketOptions();
        socketOptions.SetKeepAlive(true);
        socketOptions.SetConnectTimeoutMillis(cqlConfiguration.SocketConnectionTimeout);
        socketOptions.SetReadTimeoutMillis(cqlConfiguration.SocketReadTimeout);

        var cluster = Cluster.Builder()
            .WithCloudSecureConnectionBundle(savePath)
            .WithCredentials(clientId, clientSecret)
            .WithReconnectionPolicy(new ExponentialReconnectionPolicy(cqlConfiguration.ReconnectBaseDelayMs,
                cqlConfiguration.ReconnectMaxDelayMs))
            .WithRetryPolicy(new LoggingRetryPolicy(new DefaultRetryPolicy()))
            .WithCompression(CompressionType.Snappy)
            .WithApplicationName(cqlConfiguration.ApplicationName)
            .WithApplicationVersion(Environment.GetEnvironmentVariable("APPLICATION_VERSION") ?? "0.0.0")
            .WithQueryTimeout(cqlConfiguration.QueryTimeout)
            .WithSocketOptions(socketOptions);

        if (cqlConfiguration.UseSsl)
        {
            cluster = cluster.WithSSL();
        }

        services.AddSingleton(typeof(ISession), cluster.Build().Connect(cqlConfiguration.KeySpace));

        return services.AddSingleton<ICqlEntityService, CqlService>();
    }
}
