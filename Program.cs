using Topshelf;

var exitCode = HostFactory.Run(hostConfigurator =>
{
    hostConfigurator.Service<Awaker.Awaker>(hostSettings =>
    {
        hostSettings.ConstructUsing(awaker => new Awaker.Awaker());
        hostSettings.WhenStarted(awaker => awaker.Start());
        hostSettings.WhenStopped(awaker => awaker.Stop());
    });

    hostConfigurator.RunAsLocalSystem();
    hostConfigurator.SetServiceName("AwakerService");
    hostConfigurator.SetDisplayName("Awaker");
    hostConfigurator.SetDescription("Awaker enables notifications for the Windows Alarm app and " +
                                    "the Alarm Clock Hd app (if exists) when system resumes after the sleep mode.");
});

var exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
Environment.ExitCode = exitCodeValue;