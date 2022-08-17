using System.Reflection;
using Microsoft.Win32;

namespace Awaker;

public class Awaker
{
    private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;
    private static readonly string ExecutablePath = @$"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\{AppName}.exe";
    private const string NotificationsSettings = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    private const string WindowsAlarms = "Microsoft.WindowsAlarms_8wekyb3d8bbwe!App";
    private const string AlarmClockHd = "AntaraSoftware.AlarmClockHD_7jhd16s0b93qm!App";

    public void Start()
    {
        Console.WriteLine("Starting");
        ResetNotifications();
        SystemEvents.PowerModeChanged -= OnPowerChange;
        SystemEvents.PowerModeChanged += OnPowerChange;
    }

    public void Stop()
    {
        Console.WriteLine("Stopping");
        ResetNotifications();
        SystemEvents.PowerModeChanged -= OnPowerChange;
    }

    private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
    {
        Console.WriteLine($"Power change detected: {e.Mode}");
        if (e.Mode != PowerModes.Resume)
            return;
        ResetNotifications();
    }

    private void ResetNotifications()
    {
        foreach (var user in WindowsIdentityHelper.GetLoggedOnUsers())
        {
            var userSid = user.Owner.Value;

            using var windowsAlarmRegKey = Registry.Users.OpenSubKey(
            @$"{userSid}\{NotificationsSettings}\{WindowsAlarms}",
            true);

            if (windowsAlarmRegKey != null)
            {
                windowsAlarmRegKey.DeleteValue("Enabled", false);
                Console.WriteLine("Windows Alarms Enabled");
                windowsAlarmRegKey.Close();
            }

            using var alarmClockHdRegKey = Registry.Users.OpenSubKey(
                @$"{userSid}\{NotificationsSettings}\{AlarmClockHd}",
                true);

            if (alarmClockHdRegKey != null)
            {
                alarmClockHdRegKey.DeleteValue("Enabled", false);
                Console.WriteLine("Alarm Clock Hd Enabled");
                alarmClockHdRegKey.Close();
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