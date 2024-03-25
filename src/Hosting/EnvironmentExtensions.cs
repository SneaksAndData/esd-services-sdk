using System;

namespace Snd.Sdk.Hosting;

/// <summary>
/// Additional functionality for environment management.
/// </summary>
public static class EnvironmentExtensions
{
    /// <summary>
    /// Read environment variable bound to this application domain.
    /// </summary>
    /// <param name="varName">Name of environment variable bound to AppDomain to read.</param>
    /// <returns></returns>
    public static string GetDomainEnvironmentVariable(string varName) =>
        Environment.GetEnvironmentVariable($"{AppDomain.CurrentDomain.FriendlyName.ToUpperInvariant()}__{varName}") ?? "";

    /// <summary>
    /// Sets the environment variable bound to this application domain.
    /// </summary>
    /// <param name="varName">Name of environment variable bound to AppDomain to set.</param>
    /// <param name="varValue">Value of environment variable bound to AppDomain to set.</param>
    /// <returns></returns>
    public static void SetDomainEnvironmentVariable(string varName, string varValue) =>
        Environment.SetEnvironmentVariable($"{AppDomain.CurrentDomain.FriendlyName.ToUpperInvariant()}__{varName}", varValue);

    /// <summary>
    /// Returns the name prefix used for domain variables.
    /// </summary>
    /// <returns></returns>
    public static string GetDomainVariablePrefix() => $"{AppDomain.CurrentDomain.FriendlyName.ToUpperInvariant()}__";
}
