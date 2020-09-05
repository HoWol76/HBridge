using System;
using System.Diagnostics;

namespace Shutters
{
    internal class Logger
    {
        private static readonly int ShuttersEvent = 1;

        private static TraceSource mySource =
                new TraceSource("ShuttersTraceSource");

        internal static void LogVerbose(string message)
        {
            mySource.TraceEvent(TraceEventType.Verbose, ShuttersEvent, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: {message}");
            mySource.Flush();
        }

        internal static void Log(string message)
        {
            mySource.TraceEvent(TraceEventType.Information, ShuttersEvent, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: {message}");
            mySource.Flush();
        }
    }
}