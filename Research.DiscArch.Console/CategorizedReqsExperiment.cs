using Research.DiscArch.Designer;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Console;

public class CategorizedReqsExperiment
{
    public async void Run()
    {
        //var messagingSystemReqs = ResourceManager.LoadRequirments(SystemNames.Messaging);
        var reqs = ResourceManager.LoadRequirments(SystemNames.Contradictory);
        ReqParsing.RequirementParser parser = new();
        parser.LoadFromText(reqs);

        await parser.Parse();

        var asr = parser.Requirements.Where(r => r.Parsed && r.IsArchitecturallySignificant).ToList();

        foreach (var requirement in asr)
        {
            System.Console.WriteLine(requirement.Description);

            System.Console.WriteLine($"- Quality: {string.Join(",", requirement.QualityAttributes)}");
            if (requirement.ConditionText != null)
            {
                System.Console.WriteLine($"- Condition: {requirement.ConditionText}");
            }
            if (requirement.MetricTriggers.Any())
            {
                System.Console.WriteLine($"- Metrics: {string.Join('\n', requirement.MetricTriggers.Select(m => m.ToString()))}");
            }
        }

        var concerns = (await new Architect().SelectArch(asr)).ToList();
        foreach (var concern in concerns)
        {
            System.Console.WriteLine($"Concern {concerns.IndexOf(concern)}\n");
            System.Console.WriteLine(concern);
            System.Console.WriteLine();
        }

        System.Console.ReadLine();
    }
}
