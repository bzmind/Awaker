using System.Diagnostics;

namespace Awaker;

public static class Logger
{
    private const string EventLogSource = "Awaker Service";
    private const string EventLogName = "Awaker";

    public static void Log(string message)
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
            eventLog.WriteEntry(message, EventLogEntryType.Information);
        }
        catch (Exception e)
        {
            var error = e.Message + Environment.NewLine + e.StackTrace;
            using var eventLog = new EventLog("Application");
            eventLog.Source = EventLogSource;
            eventLog.WriteEntry(error, EventLogEntryType.Error);
        }
    }

    public static void ClearLogsIfMoreThan(int amount)
    {
        try
        {
            if (!EventLog.SourceExists(EventLogSource))
                return;
            using var eventLog = new EventLog(EventLogName);
            eventLog.Source = EventLogSource;
            if (eventLog.Entries.Count >= amount)
                eventLog.Clear();
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