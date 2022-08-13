# Awaker
I use an app called Alarm Cock HD for daily reminders, which I think it relies on the Windows Alarms app, and the problem "was" that they both would get disabled by windows, whenever the computer would go into sleep mode, so I'd miss some reminders and most of the time I didn't feel like it to go into windows notifications settings and enable both of them manually, so I did what every programmer does, I automated it :o)
<br/>
It is done by making an "Enabled" key with a value of 1, for each of these apps in their registery keys under this path:
<br/>
`CHKEY_USERS\{YourSID}\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\{AppFolderName}`.
<br/>
<br/>
So in my case they look like this:
<br/>
Alarm Clock Hd: `HKEY_USERS\S-1-5-21-3773018586-4214865330-3152829066-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\AntaraSoftware.AlarmClockHD_7jhd16s0b93qm!App`
<br/>
![AlarmClockHd](https://user-images.githubusercontent.com/77694696/184496276-067ccfd1-0b52-4c43-9f82-8b23ac2516bd.png)
<br/>
<br/>
Windows Alarms: `HKEY_USERS\S-1-5-21-3773018586-4214865330-3152829066-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.WindowsAlarms_8wekyb3d8bbwe!App`
<br/>
![WindowsAlarms](https://user-images.githubusercontent.com/77694696/184496350-eabe2af0-680c-4139-ac72-611fca8fb6b4.png)
<br/>
<br/>
The Enabled key is made/edited (and then set to 0 and then 1) once the service starts, and then on PowerChange event and then when the service stops. The PowerChange event will trigger when the computer goes into sleep mode or resumes to work after being in sleep mode, in this case, I only re-enable the apps on the Resume mode, because that's when the notifications would stop working.
