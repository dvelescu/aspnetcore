// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting;

internal class WebHostOptions
{
    public WebHostOptions(IConfiguration configuration, string applicationNameFallback)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        ApplicationName = configuration[WebHostDefaults.ApplicationKey] ?? applicationNameFallback;
        StartupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];
        DetailedErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
        CaptureStartupErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
        Environment = configuration[WebHostDefaults.EnvironmentKey];
        WebRoot = configuration[WebHostDefaults.WebRootKey];
        ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
        PreventHostingStartup = WebHostUtilities.ParseBool(configuration, WebHostDefaults.PreventHostingStartupKey);
        SuppressStatusMessages = WebHostUtilities.ParseBool(configuration, WebHostDefaults.SuppressStatusMessagesKey);

        // Search the primary assembly and configured assemblies.
        HostingStartupAssemblies = Split(ApplicationName, configuration[WebHostDefaults.HostingStartupAssembliesKey]);
        HostingStartupExcludeAssemblies = Split(configuration[WebHostDefaults.HostingStartupExcludeAssembliesKey]);

        var timeout = configuration[WebHostDefaults.ShutdownTimeoutKey];
        if (!string.IsNullOrEmpty(timeout)
            && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
        {
            ShutdownTimeout = TimeSpan.FromSeconds(seconds);
        }
    }

    public string ApplicationName { get; set; }

    public bool PreventHostingStartup { get; set; }

    public bool SuppressStatusMessages { get; set; }

    public IReadOnlyList<string> HostingStartupAssemblies { get; set; }

    public IReadOnlyList<string> HostingStartupExcludeAssemblies { get; set; }

    public bool DetailedErrors { get; set; }

    public bool CaptureStartupErrors { get; set; }

    public string? Environment { get; set; }

    public string? StartupAssembly { get; set; }

    public string? WebRoot { get; set; }

    public string? ContentRootPath { get; set; }

    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public IEnumerable<string> GetFinalHostingStartupAssemblies()
    {
        return HostingStartupAssemblies.Except(HostingStartupExcludeAssemblies, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> Split(string? value)
    {
        return value?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();
    }

    private static IReadOnlyList<string> Split(string applicationName, string? environment)
    {
        if (string.IsNullOrEmpty(environment))
        {
            return new[] { applicationName };
        }

        return Split($"{applicationName};{environment}");
    }
}
