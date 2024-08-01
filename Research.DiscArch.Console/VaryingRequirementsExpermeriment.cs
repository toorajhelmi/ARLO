using System.Collections.Generic;
using Research.DiscArch.Designer;
using Research.DiscArch.Models;

namespace Research.DiscArch.Console
{
    public class VaryingRequirementsExpermeriment : RequirementBasedExperiment
    {
        public VaryingRequirementsExpermeriment(ExperimentSettings experimentSettings)
            : base(experimentSettings)
        {
            experimentSettings.ExperimentKind = ExperimentKind.Varying;
        }

        public async void Run()
        {
            await LoadRequirements();

            var asrsCopy = new List<Requirement>();
            asrsCopy.AddRange(asrs);

            experimentSettings.LoadConditionGroupsFromFile = false;

            //Do one round without removing anything
            await Decide();

            var removalStep = 0.2;
            var removeCount = (int)(asrsCopy.Count * removalStep);

            for (var removalRatio = 0.0; removalRatio < 1; removalRatio += removalStep)
            {
                var couldRemoveAny = RemoveAsrs(removeCount);
                if (!couldRemoveAny)
                {
                    reportingService.Writeline("!!!!!!! No more ASRs to exclude. Terminating the experiment !!!!!!", Services.Verbosity.Results);
                    break;
                }

                reportingService.Writeline();
                reportingService.Writeline("===================================================", Services.Verbosity.Results);
                reportingService.Writeline($" Experiment Begin - Removed {removalRatio}", Services.Verbosity.Results);
                reportingService.Writeline("===================================================", Services.Verbosity.Results);
                reportingService.Writeline();
                await Decide();

                System.Console.WriteLine("Done!");
                System.Console.ReadLine();
            }
        }

        private async Task Decide()
        {
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
        }

        private bool RemoveAsrs(int removeCount)
        {
            reportingService.Writeline("Removing ASRs", Services.Verbosity.Results);
            var asrsToRemove = asrs.OrderBy(_ => Random.Shared.Next()).ToList();

            if (experimentSettings.RemovalStratgy.Kind == RemovalStrategy.RemovalKind.Targeted)
            {
                asrsToRemove = asrs.Where(a => !a.QualityAttributes.Any(q => experimentSettings.RemovalStratgy.DesiredQAs.Contains(q)) && a.QualityAttributes.Any(q => experimentSettings.RemovalStratgy.UndesiredQAs.Contains(q))).ToList();
            }

            removeCount = Math.Min(removeCount, asrsToRemove.Count());

            if (removeCount == 0)
                return false;

            for (int i = removeCount - 1; i >= 0; i--)
            {
                var asrToRemove = asrsToRemove[i];
                asrsToRemove.Remove(asrToRemove);
                asrs.Remove(asrToRemove);

                reportingService.Writeline(asrToRemove.ToString());
                if (asrs.Count == 0 || asrsToRemove.Count == 0)
                    return true;
            }

            return true;
        }
    }
}

