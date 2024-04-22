using System;
using System.Reflection;

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
    /// <param name="defaultValue">Optional default value to provide.</param> 
    /// <returns></returns>
    public static string GetDomainEnvironmentVariable(string varName, string defaultValue = "") =>
        Environment.GetEnvironmentVariable($"{AppDomain.CurrentDomain.FriendlyName.ToUpperInvariant()}__{varName}") ?? defaultValue;

    /// <summary>
    /// Sets the environment variable bound to this assembly domain.
    /// </summary>
    /// <param name="varName">Name of environment variable bound to Assembly to set.</param>
    /// <param name="varValue">Value of environment variable bound to Assembly to set.</param>
    /// <returns></returns>
    public static void SetAssemblyEnvironmentVariable(string varName, string varValue) =>
        Environment.SetEnvironmentVariable($"{GetAssemblyVariablePrefix()}{varName}", varValue);

    /// <summary>
    /// Read environment variable bound to this assembly domain.
    /// </summary>
    /// <param name="varName">Name of environment variable bound to Assembly to read.</param>
    /// <param name="defaultValue">Optional default value to provide.</param> 
    /// <returns></returns>
    public static string GetAssemblyEnvironmentVariable(string varName, string defaultValue = "") =>
        Environment.GetEnvironmentVariable($"{GetAssemblyVariablePrefix()}{varName}") ?? defaultValue;

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

    /// <summary>
    /// Returns the name prefix used for assembly variables.
    /// </summary>
    /// <returns></returns>
    public static string GetAssemblyVariablePrefix()
    {
        var name = Assembly.GetExecutingAssembly().GetName().Name ??
                   throw new InvalidOperationException("Assembly name not found.");
        return $"{name.ToUpperInvariant()}__";
    }
}
