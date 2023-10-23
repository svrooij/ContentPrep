using Microsoft.Extensions.Logging;
using System;
using System.Management.Automation;

namespace SvR.ContentPrep.Cmdlet
{
    internal class PowerShellLogger<T> : ILogger<T>
    {
        private readonly LogLevel _logLevel;
#nullable enable
        private readonly PSCmdlet? _cmdlet;

        public PowerShellLogger(PSCmdlet? cmdlet = null)
        {
            _logLevel = LogLevel.Debug;
            _cmdlet = cmdlet;
        }

#nullable disable

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _cmdlet?.WriteLog(logLevel, formatter(state, exception), exception);
        }
    }

    internal class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
    }

    internal static class PsCmdletExtensions
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public static void WriteLog(this PSCmdlet cmdlet, LogLevel logLevel, string message, Exception? e = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    cmdlet.WriteVerbose(message);
                    break;

                case LogLevel.Debug:
                    cmdlet.WriteDebug(message);
                    break;

                case LogLevel.Information:
                    cmdlet.WriteInformation(message, new string[] { });
                    Console.WriteLine($"INFO: {message}");
                    break;

                case LogLevel.Warning:
                    cmdlet.WriteWarning(message);
                    break;

                case LogLevel.Error:
                    cmdlet.WriteError(new ErrorRecord(e ?? new Exception(message), "1", ErrorCategory.InvalidOperation, null));
                    break;
            }
        }
    }
}