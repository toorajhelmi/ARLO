using Research.DiscArch.ReqParsing;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Console;

public class CategorizedKaggleReqsExperiment
{
    public async void Run()
    {
        var kaggleReqs = ResourceManager.LoadRequirments(SystemNames.Kaggle);
        RequirementParser parser = new();
        parser.LoadFromText(kaggleReqs);

        await parser.Parse();

        foreach (var requirement in parser.Requirements.Where(r => r.Parsed))
        {
            System.Console.WriteLine($"{requirement.Id}: {requirement.IsArchitecturallySignificant} {requirement.Description}");
            if (requirement.IsArchitecturallySignificant)
            {
                System.Console.WriteLine($"Quality: {string.Join(",", requirement.QualityAttributes)}");
                System.Console.WriteLine($"Condition: {requirement.ConditionText}");
                //System.Console.WriteLine($"Metric: {requirement.Metric}");
            }
        }
    }
}
