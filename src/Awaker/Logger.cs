using System.Diagnostics;

namespace Awaker;

public static class Logger
{
    private const string EventLogSource = "Awaker Service";
    private const string EventLogName = "Awaker";

    public static void Log(string message, EventLogEntryType entryType)
    {
        try
        {
            if (!EventLog.SourceExists(EventLogSource))
            {
                var eventSourceData = new EventSourceCreationData(EventLogSource, EventLogName);
                EventLog.CreateEventSource(eventSourceData);
            }

            using var eventLog = new EventLog(EventLogName);
            eventLog.Source = EventLogSource;
            eventLog.WriteEntry(message, entryType);
        }
        catch (Exception e)
        {
            var error = e.Message + Environment.NewLine + e.StackTrace;
            using var eventLog = new EventLog("Application");
            eventLog.Source = EventLogSource;
            eventLog.WriteEntry(error, EventLogEntryType.Error);
        }
    }
}