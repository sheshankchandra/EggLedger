using System;
using System.IO;
using System.Runtime.InteropServices;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Extensions;

public static class LoggingExtensions
{
    public static IHostApplicationBuilder AddApplicationLogging(this IHostApplicationBuilder builder)
    {
        // Set the EGGLEDGER_LOG_PATH environment variable based on OS
        SetLogPathEnvironmentVariable();

        // Configure log4net for file-based logging
        var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        // Clear default providers and use log4net
        builder.Logging.ClearProviders();
        builder.Logging.AddLog4Net();

        return builder;
    }

    private static void SetLogPathEnvironmentVariable()
    {
        string logPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use LocalApplicationData on Windows (e.g., C:\Users\[username]\AppData\Local\EggLedger\Logs)
            logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EggLedger", "Logs");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Use /var/log/eggledger on Linux, fallback to user home if no permissions
            logPath = "/var/log/eggledger";
            
            // Check if we can write to /var/log, if not, use user's home directory
            try
            {
                Directory.CreateDirectory(logPath);
                // Test write permissions
                var testFile = Path.Combine(logPath, "test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (UnauthorizedAccessException)
            {
                // Fallback to user's home directory if we don't have permissions for /var/log
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                logPath = Path.Combine(homeDir, ".local", "share", "eggledger", "logs");
            }
            catch (DirectoryNotFoundException)
            {
                // Fallback to user's home directory if /var/log doesn't exist
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                logPath = Path.Combine(homeDir, ".local", "share", "eggledger", "logs");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Use ~/Library/Logs/EggLedger on macOS
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            logPath = Path.Combine(homeDir, "Library", "Logs", "EggLedger");
        }
        else
        {
            // Fallback for other Unix-like systems
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            logPath = Path.Combine(homeDir, ".local", "share", "eggledger", "logs");
        }

        // Ensure directory exists
        try
        {
            Directory.CreateDirectory(logPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create log directory '{logPath}': {ex.Message}");
            // Last resort fallback to temp directory
            logPath = Path.Combine(Path.GetTempPath(), "EggLedger");
            Directory.CreateDirectory(logPath);
        }

        // Set the environment variable for log4net to use
        Environment.SetEnvironmentVariable("EGGLEDGER_LOG_PATH", logPath);
        
        Console.WriteLine($"Log path set to: {logPath}");
    }
}