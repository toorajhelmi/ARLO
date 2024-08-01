using Research.DiscArch.Models;
using Research.DiscArch.Services;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Console
{
    public class RequirementBasedExperiment
	{
        protected List<Requirement> requirements;
        protected List<Requirement> asrs;
        protected IReportingService reportingService;
        protected ExperimentSettings experimentSettings;

        public RequirementBasedExperiment(ExperimentSettings experimentSettings)
        {
            this.experimentSettings = experimentSettings;
            reportingService = new FileReportingService(experimentSettings, Verbosity.Results);
        }

        protected async Task LoadRequirements()
        {
            reportingService.Writeline($"Date/Time: {DateTime.Now}");
            var requirements = ResourceManager.LoadRequirments(experimentSettings.System);

            reportingService.Writeline();
            reportingService.Writeline($"Settings:\n" +
                $"System Name: {experimentSettings.ExpermentName}\n" +
                $"Optimization Strategy: {experimentSettings.OptimizationStrategy}\n" +
                $"Quality Weights Mode: {experimentSettings.QualityWeightsMode}");

            reportingService.Writeline();
            reportingService.Writeline("Requirements");
            reportingService.Writeline(requirements);

            ReqParsing.RequirementParser parser = new(reportingService);
            parser.LoadFromText(requirements);

            await parser.Parse(experimentSettings);

            asrs = parser.Requirements.Where(r => r.Parsed && r.IsArchitecturallySignificant).ToList();

            reportingService.Writeline();
            reportingService.Writeline("ASRs:");

            foreach (var requirement in asrs)
            {
                reportingService.Writeline(requirement.Description);

                reportingService.Writeline($"- Quality: {string.Join(",", requirement.QualityAttributes)}");
                if (requirement.ConditionText != null)
                {
                    reportingService.Writeline($"- Condition: {requirement.ConditionText}");
                }
                if (requirement.MetricTriggers.Any())
                {
                    reportingService.Writeline($"- Metrics: {string.Join('\n', requirement.MetricTriggers.Select(m => m.ToString()))}");
                }
            }
        }
    }
}

