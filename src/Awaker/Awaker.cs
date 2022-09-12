using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Topshelf;

namespace Awaker;

[SupportedOSPlatform("windows")]
public class Awaker
{
    private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;
    private static readonly string ExecutablePath = @$"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\{AppName}.exe";
    private const string NotificationsSettings = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    private const string WindowsAlarms = "Microsoft.WindowsAlarms_8wekyb3d8bbwe!App";
    private const string AlarmClockHd = "AntaraSoftware.AlarmClockHD_7jhd16s0b93qm!App";

    public void Start()
    {
        Logger.Log("Starting the service", EventLogEntryType.Information);
        ResetNotifications();
    }

    public void Stop()
    {
        Logger.Log("Stopping the service", EventLogEntryType.Information);
    }

    public bool OnPowerChange(PowerEventArguments e)
    {
        Logger.Log($"Power change detected: {e.EventCode}", EventLogEntryType.Information);
        if (e.EventCode != PowerEventCode.ResumeSuspend)
            return false;
        ResetNotifications();
        return true;
    }

    private void ResetNotifications()
    {
        foreach (var user in WindowsIdentityHelper.GetLoggedOnUsers())
        {
            var userSid = user.Owner.Value;
            Logger.Log($"User SID: {userSid}", EventLogEntryType.Information);

            using var alarmClockHdRegKey = Registry.Users
                .OpenSubKey(@$"{userSid}\{NotificationsSettings}\{AlarmClockHd}", true);

            Logger.Log($"Is AlarmClockHdRegistryKey null: {alarmClockHdRegKey == null}",
                EventLogEntryType.Information);

            if (alarmClockHdRegKey != null)
            {
                alarmClockHdRegKey.DeleteValue("Enabled", false);
                alarmClockHdRegKey.SetValue("Enabled", 0, RegistryValueKind.DWord);
                alarmClockHdRegKey.DeleteValue("Enabled", false);
                var enabled = alarmClockHdRegKey.GetValue("Enabled") == null;
                if (enabled)
                    Logger.Log("Enabled Alarm Clock Hd", EventLogEntryType.SuccessAudit);
                else
                    Logger.Log("Could not enable Alarm Clock Hd", EventLogEntryType.FailureAudit);
                alarmClockHdRegKey.Close();
            }

            using var windowsAlarmRegKey = Registry.Users
                .OpenSubKey(@$"{userSid}\{NotificationsSettings}\{WindowsAlarms}", true);

            Logger.Log($"Is WindowsAlarmRegistryKey null: {windowsAlarmRegKey == null}",
                EventLogEntryType.Information);

            if (windowsAlarmRegKey != null)
            {
                windowsAlarmRegKey.DeleteValue("Enabled", false);
                windowsAlarmRegKey.SetValue("Enabled", 0, RegistryValueKind.DWord);
                windowsAlarmRegKey.DeleteValue("Enabled", false);
                var enabled = windowsAlarmRegKey.GetValue("Enabled") == null;
                if (enabled)
                    Logger.Log("Enabled Windows Alarms", EventLogEntryType.SuccessAudit);
                else
                    Logger.Log("Could not enable Windows Alarms", EventLogEntryType.FailureAudit);
                windowsAlarmRegKey.Close();
            }
        }
    }

    private void SetAsStartup()
    {
        using var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (regKey != null)
        {
            var previousValue = regKey.GetValue(AppName);
            if (previousValue == null || previousValue.ToString() != ExecutablePath)
            {
                regKey.SetValue(AppName, ExecutablePath);
                regKey.Close();
            }
        }
    }
}