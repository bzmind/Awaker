# Awaker
### How to use:
Run CMD as administrator, change your directory to "src\Awaker\bin\Debug\net6.0", and install the service:
<br/>
```batch
> Awaker.exe install start
```
### Why I made it:
I use an app called Alarm Cock HD for daily reminders, which I think it relies on the Windows Alarms app, and the problem was that they both would get disabled by windows whenever the computer would go into sleep mode (I'm not sure, but mostly after the computer being in sleep mode, they would get disabled), so I'd miss some reminders and most of the time I didn't feel like it to go into windows notifications settings and enable both of them manually, so I did what every programmer does, I automated it :o)
<br/>
By creating a windows service based on [this](https://youtu.be/y64L-3HKuP0) tutorial which uses the [Topshelf](https://www.nuget.org/packages/Topshelf/) nuget package that makes it easier to create windows services with C#.
<br/>
### How it works:
In general, it is done by removing the "Enabled" key for each of these apps in their registery keys under this path:
<br/>
`CHKEY_USERS\{YourSID}\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\{AppFolderName}`
<br/>
<br/>
So in my case their paths look like this:
<br/>
Alarm Clock Hd: `HKEY_USERS\S-1-5-21-3773018586-4214865330-3152829066-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\AntaraSoftware.AlarmClockHD_7jhd16s0b93qm!App`
<br/>
Windows Alarms: `HKEY_USERS\S-1-5-21-3773018586-4214865330-3152829066-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.WindowsAlarms_8wekyb3d8bbwe!App`
<br/>
<br/>
The service will try to remove the Enabled key once the service starts, and then on PowerChange event and then when the service stops. The PowerChange event will trigger when the computer goes into sleep mode or resumes to work after being in sleep mode, in this case, I only use the Resume mode, because that's when the notifications would stop working.
<br/>
<br/>
But the SID of every computer (and it's user accounts) is different, so to grab the SID of all the current logged in users, I fortunately encountered [this](https://www.softwaremeadows.com/posts/writing-to-current-users-registry-when/) blog, and I used the WindowsIdentityHelper class introduced in there. So by using that we grab the SID of all logged in users, and then we can navigate to each user's registery keys and then delete the Enabled value for each app.
