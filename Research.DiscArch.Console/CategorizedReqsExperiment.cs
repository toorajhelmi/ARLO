using Research.DiscArch.Designer;
using Research.DiscArch.Models;
using Research.DiscArch.Services;

namespace Research.DiscArch.Console;

public class CategorizedReqsExperiment : RequirementBasedExperiment
{
    public CategorizedReqsExperiment(ExperimentSettings experimentSettings)
            : base(experimentSettings)
    {
        experimentSettings.ExperimentKind = ExperimentKind.BasicArlo;
    }

    public async void Run()
    {
        await LoadRequirements();

        var concerns = (await new Architect(reportingService, experimentSettings, asrs).SelectArch()).ToList();

        reportingService.Writeline();
        reportingService.Writeline("Concerns");

        foreach (var concern in concerns)
        {
            reportingService.Writeline($"Concern {concerns.IndexOf(concern)}\n");
            reportingService.Writeline(concern.ToString());
            reportingService.Writeline();
        }

        reportingService.WriteStats();

        System.Console.WriteLine("Done!");
        System.Console.ReadLine();
    }
}
