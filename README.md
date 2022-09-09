# Awaker
### How to use:
Run CMD as administrator, change your directory to "src\Awaker\bin\Debug\net6.0", and install the service:
<br/>
```batch
> Awaker.exe install start
```
And to disable it, navigate to that same folder, and run this:
```batch
> Awaker.exe stop uninstall
```
I use Windows Event Viewer for logging, you can view the logs by opening the Event Viewer and then expanding the Applicatons and Services Logs section:
![image](https://user-images.githubusercontent.com/77694696/189342715-9c4b3b9b-7446-4565-9519-f45294be0071.png)
### Why I made it:
I use an app called Alarm Cock HD for daily reminders, which I think relies on the Windows Alarms app, and the problem was that they both would get disabled by windows whenever the computer would go into sleep mode (I'm not sure, but mostly after the computer is in sleep mode, they would get disabled), so I'd miss some reminders and most of the time I didn't feel like it to go into windows notifications settings and enable both of them manually, so I did what every programmer does, I automated it :o)
<br/>
By creating a windows service based on [this](https://youtu.be/y64L-3HKuP0) tutorial which uses the [Topshelf](https://www.nuget.org/packages/Topshelf/) nuget package that makes it easier to create windows services with C#.
<br/>
### How it works:
In general, it is done by removing the "Enabled" key for each of these alarm apps in their registry keys. Removing a key named "Enabled" in order to enable the app might sound weird, but that's how Windows works, so by default, if you disable an app's notifications manually in Windows, Windows will make a key named "Enabled" but with a value of 0, which actually disables that app's notifications, so if you want it to be enabled, then there should be no key named as "Enabled" in the app's registry keys, so that's why we remove that key, the registry keys related to apps notifications is located in this path:
<br/>
`CHKEY_USERS\{YourSID}\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\{AppFolderName}`
<br/>
<br/>
So in my case, their paths look like this:
<br/>
Alarm Clock Hd: `HKEY_USERS\S-1-5-21-3773018586-4214865330-3152829066-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\AntaraSoftware.AlarmClockHD_7jhd16s0b93qm!App`
<br/>
Windows Alarms: `HKEY_USERS\S-1-5-21-3773018586-4214865330-3152829066-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.WindowsAlarms_8wekyb3d8bbwe!App`
<br/>
<br/>
The service will try to remove the Enabled key once the service starts, and also on the Windows power status change event, this event will trigger whenever a change has occurred in the power status of Windows, like when it goes into sleep mode or resumes after being in sleep mode. In this case, I only use the Resume mode, because that's when the notifications would stop working (I guess).
<br/>
<br/>
Now, the SID of every computer (and its user accounts) is different, so to grab the SID of all the current logged in users, I fortunately encountered [this](https://www.softwaremeadows.com/posts/writing-to-current-users-registry-when/) blog, and I used the WindowsIdentityHelper class introduced in there. So by using that we grab the SID of all logged in users, and then we can navigate to each user's registry keys and then delete the Enabled value for each of the alarms.
