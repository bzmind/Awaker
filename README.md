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
#### Step 1: Enable alarm apps from their registries
To do that, we actually need to remove the "Enabled" key for each of these alarm apps in their registry keys. Removing a key named "Enabled" in order to enable the app might sound weird, but that's how Windows works, so by default, if you disable an app's notifications manually in Windows, Windows will make a key named "Enabled" but with a value of 0, which actually disables that app's notifications, so if you want it to be enabled, then there should be no key named as "Enabled" in the app's registry keys, so that's why we remove that key, the registry keys related to apps notifications is located in this path:
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
The service will try to remove the Enabled key once the service starts, and also when the Windows power status change event occurres, this event will trigger whenever a change has occurred in the power status of Windows, like when it goes into sleep mode or resumes after being in sleep mode. In this case, I only use the Resume mode, because that's when the notifications would stop working (I guess).
<br/>
<br/>
Now, the SID of every computer (and its user accounts) is different, so to grab the SID of all the current logged in users, I fortunately encountered [this](https://www.softwaremeadows.com/posts/writing-to-current-users-registry-when/) blog, and I used the WindowsIdentityHelper class introduced in there. So by using that we grab the SID of all logged in users, and then we can navigate to each user's registry keys and then delete the Enabled key for each of the alarm apps.
#### Step 2: Enable alarm apps from Windows notifications database
After I edited their registries, I thought all of it is done, but it wasn't, after searching for a while, I encountered [this](https://docs.microsoft.com/en-us/answers/questions/314561/enforcing-notifications-from-specific-applications.html) question which suggests that Windows also has a database which controls which apps can send toast notifications, so I guess enabling them with registry keys would fix half of the problem, they were sorta enabled, but not completely because their toast notifications wouldn't appear. The database is located under this path: `C:\Users\Your_Username\AppData\Local\Microsoft\Windows\Notifications\wpndatabase.db`
<br/>
<br/>
So I had to write some SQLite queries to set their `s:toast` values from 0 back to 1, this value specifies if the app can send toast notifications or not, so to do that I used some of the codes from that question above, and I changed it a little bit, and I ended up with this query:
<br/>
```SQL
UPDATE HandlerSettings AS HS
SET Value = 1
WHERE EXISTS
(
  SELECT *
  FROM NotificationHandler AS NH
  WHERE (NH.PrimaryId LIKE '%AlarmClockHD%' OR NH.PrimaryId LIKE '%WindowsAlarms%')
  AND HS.SettingKey = 's:toast' AND NH.RecordId = HS.HandlerId
)
```
Now the problem was the connection string, so I had to point to this location: `C:\Users\Your_Username\AppData\Local\Microsoft\Windows\Notifications\wpndatabase.db` and I didn't want to hard code the username, so I didn't even mention the username, I tried to use the Windows environment variables, like %APPDATA%. To access those in C#, you can use `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` which gives you this path `C:\Users\Your_Username\AppData\Local` and then you can append the rest of the database path to it. That easy? NO!, the problem is that the service runs as the Local System account, which is different than a regular Windows account, I don't know much about these accounts, I think there were other ways to fix this problem, like changing your service's account to run as a regular account, but I didn't put much time on searching for that (because I had some difficulties doing that a while ago), so the problem with the Local System was that if you'd run the same C# code to grab the path: `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` it wouldn't give you the same folder as before, I don't remember but it was somewhere under system32 subfolders, not what we want! so, I searched about how to grab this location from a Local System account, and I found [this](https://stackoverflow.com/questions/11201308/get-appdata-local-folder-path-in-c-sharp-windows-service) question on SO, forget about the first answer, the second answer (as of now) was what I needed, it is saying that you can grab these paths from the registry keys as well!
<br/>
Here's the location for those paths:
`HKEY_USERS\{YourSid}\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders` on the right hand side you can view all the paths, so I used the same way as step 1, I grabbed the SID of current logged on users and navigated to this path and used the "Local AppData" registry key's value to grab the path to AppData folder of the user, and then appended the rest of the database path to it. Now that I have the full connection string I just execute the query above after removing the Enabled registry keys.
