using System.Data.SQLite;
using System.Reflection;
using System.Runtime.Versioning;
using System.Timers;
using Dapper;
using Microsoft.Win32;
using Topshelf;

namespace Awaker;

[SupportedOSPlatform("windows")]
public class Awaker
{
    private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;
    private static readonly string ExecutablePath = @$"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\{AppName}.exe";
    private const string ShellFolders = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";
    private const string NotificationsSettings = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    private const string WindowsAlarms = "Microsoft.WindowsAlarms_8wekyb3d8bbwe!App";
    private const string AlarmClockHd = "AntaraSoftware.AlarmClockHD_7jhd16s0b93qm!App";

    public void Start()
    {
        Logger.ClearLogsIfMoreThan(100);
        Logger.Log("Starting the service");
        if (!WindowsIdentityHelper.GetLoggedOnUsers().Any())
        {
            Logger.Log("No logged in user found, waiting for 5 seconds to try again...");
            Pause(5000, (o, e) => Start());
            return;
        }
        EnableNotificationsInRegistries();
        EnableNotificationsInWindowsNotificationsDatabase();
    }

    public void Stop()
    {
        Logger.Log("Stopping the service");
        EnableNotificationsInRegistries();
        EnableNotificationsInWindowsNotificationsDatabase();
    }

    public bool OnPowerChange(PowerEventArguments e)
    {
        Logger.Log($"Power change detected: {e.EventCode}");
        if (e.EventCode != PowerEventCode.ResumeSuspend)
            return false;
        EnableNotificationsInRegistries();
        EnableNotificationsInWindowsNotificationsDatabase();
        return true;
    }

    private void EnableNotificationsInRegistries()
    {
        Logger.Log("Trying to enable alarms notifications from their registry keys...");

        foreach (var user in WindowsIdentityHelper.GetLoggedOnUsers())
        {
            var userSid = user.Owner.Value;
            Logger.Log($"User SID: {userSid}");

            using var alarmClockHdRegistry = Registry.Users
                .OpenSubKey(@$"{userSid}\{NotificationsSettings}\{AlarmClockHd}", true);

            Logger.Log(alarmClockHdRegistry != null
                ? "Alarm Clock Hd registry found" : "Alarm Clock Hd registry not found");

            if (alarmClockHdRegistry != null)
            {
                alarmClockHdRegistry.DeleteValue("Enabled", false);
                alarmClockHdRegistry.SetValue("Enabled", 0, RegistryValueKind.DWord);
                alarmClockHdRegistry.DeleteValue("Enabled", false);
                Logger.Log(alarmClockHdRegistry.GetValue("Enabled") == null
                    ? "Enabled Alarm Clock Hd" : "Could not enable Alarm Clock Hd");
                alarmClockHdRegistry.Close();
            }

            using var windowsAlarmRegistry = Registry.Users
                .OpenSubKey(@$"{userSid}\{NotificationsSettings}\{WindowsAlarms}", true);

            Logger.Log(windowsAlarmRegistry != null
                ? "Windows Alarm registry found" : "Windows Alarm registry not found");

            if (windowsAlarmRegistry != null)
            {
                windowsAlarmRegistry.DeleteValue("Enabled", false);
                windowsAlarmRegistry.SetValue("Enabled", 0, RegistryValueKind.DWord);
                windowsAlarmRegistry.DeleteValue("Enabled", false);
                Logger.Log(windowsAlarmRegistry.GetValue("Enabled") == null
                    ? "Enabled Windows Alarms" : "Could not enable Windows Alarms");
                windowsAlarmRegistry.Close();
            }
        }
    }

    private void EnableNotificationsInWindowsNotificationsDatabase()
    {
        Logger.Log("Trying to enable alarms notifications from Windows notifications database...");

        foreach (var user in WindowsIdentityHelper.GetLoggedOnUsers())
        {
            var databaseDirectory = "";
            var userSid = user.Owner.Value;
            Logger.Log($"User SID: {userSid}");

            using var shellFoldersRegistry = Registry.Users
                .OpenSubKey(@$"{userSid}\{ShellFolders}", true);

            Logger.Log(shellFoldersRegistry != null
                ? "Shell Folders registry found" : "Shell Folders registry not found");

            if (shellFoldersRegistry != null)
            {
                var appDataPath = shellFoldersRegistry.GetValue("Local AppData");
                Logger.Log(shellFoldersRegistry.GetValue("Local AppData") == null
                    ? "AppData registry key not found" : "AppData registry key found");
                if (appDataPath != null)
                    databaseDirectory = appDataPath.ToString();
                shellFoldersRegistry.Close();
            }

            var databasePath = $@"{databaseDirectory}\Microsoft\Windows\Notifications\wpndatabase.db";
            var databaseExists = File.Exists(databasePath);
            Logger.Log(databaseExists
                ? $"Database found at: {databasePath}" : $"Database not found at: {databasePath}");

            var connectionString = @$"Data Source={databasePath};Version=3;";
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                const string sql = @"
                    UPDATE HandlerSettings AS HS
                    SET Value = 1
                    WHERE EXISTS (
                    	SELECT *
                    	FROM NotificationHandler AS NH
                    	WHERE (NH.PrimaryId LIKE '%AlarmClockHD%' OR NH.PrimaryId LIKE '%WindowsAlarms%')
                    		AND HS.SettingKey = 's:toast' AND NH.RecordId = HS.HandlerId
                    )";
                connection.Execute(sql);
                Logger.Log("Enabled alarms notifications in Windows notifications database");
            }
            catch (Exception e)
            {
                Logger.Log("Error: Could not enable alarm notifications in Windows notifications database" +
                           $"{Environment.NewLine}{e}");
            }
        }
    }

    private void Pause(double timeInMilliseconds, ElapsedEventHandler callback)
    {
        var delayTimer = new System.Timers.Timer();
        delayTimer.Interval = timeInMilliseconds;
        delayTimer.AutoReset = false;
        delayTimer.Elapsed += callback;
        delayTimer.Start();
    }

    // This method is not used
    private void SetProgramToRunAsStartup()
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