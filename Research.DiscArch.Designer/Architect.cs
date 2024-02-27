using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Research.DiscArch.Models;
using Research.DiscArch.Services;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Designer;

public enum QualityWeightsMode
{
    EquallyImportant,
    AllRequired,
    Inferred,
    Provided
}

public class Architect
{
    private Matrix qualityArchPatternmatrix = new();
    private IReportingService reportingService;
    private ExperimentSettings experimentSettings;
    private List<Requirement> requirements;
    private List<ConditionGroup> conditionGroups = new();
    private List<SatisfiableGroup> satisfiableGroups = new(); 

    public Architect(IReportingService reportingService, ExperimentSettings experimentSettings, List<Requirement> requirements)
    {
        this.reportingService = reportingService;
        this.experimentSettings = experimentSettings;
        this.requirements = requirements;

        qualityArchPatternmatrix = ResourceManager.LoadArchPattenMatrix();

        Dictionary<string, int> qualityWeights = new();

        if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.EquallyImportant))
        {
            foreach (var column in qualityArchPatternmatrix.GetRows().First().Value)
            {
                qualityWeights[column.Key] = 1;
            }
        }
        else if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.AllRequired))
        {

        }
        else if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.Inferred))
        {
            foreach (var requirement in requirements)
            {
                foreach (var quality in requirement.QualityAttributes)
                {
                    if (!qualityWeights.ContainsKey(quality))
                        qualityWeights[quality] = 0;
                    qualityWeights[quality]++;
                }
            }
        }
        else if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.Inferred))
        {
            //This will be handled within the optimizer for each group
        }
        else if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.Provided))
        {
            qualityWeights = experimentSettings.ProvidedQualityWeights;
        }
    }

    public async Task<IEnumerable<Concern>> SelectArch()
    {
        var concerns = new List<Concern>();

        if (!experimentSettings.JustRunOptimization)
        {
            await ConsolidateSimilarConditions();
            await GenerateSatifiableGroups();
        }
        else
        {
            var desiredQAs = experimentSettings.ProvidedQualityWeights.Keys.ToList();
            var concern = new Concern
            {
                Conditions = satisfiableGroups.SelectMany(sg => sg.ConditionGroups.Select(cg => cg.NominalCondition)).ToList(),
                DesiredQualities = experimentSettings.ProvidedQualityWeights,
                Decisions = SelectDecisions(experimentSettings.ProvidedQualityWeights).decision
            };

            concerns.Add(concern);
        }

        foreach (var satisfialeGroup in satisfiableGroups)
        {
            var qualityAttrbutes = satisfialeGroup.ConditionGroups.SelectMany(cg => cg.Requirements.SelectMany(r => r.QualityAttributes)).ToList();

            var qualityWeights = new Dictionary<string, int>();

            foreach (var quality in qualityAttrbutes)
            {
                if (!qualityWeights.ContainsKey(quality))
                    qualityWeights[quality] = 0;
                qualityWeights[quality]++;
            }

            var concern = new Concern
            {
                Conditions = satisfiableGroups.SelectMany(sg => sg.ConditionGroups.Select(cg => cg.NominalCondition)).ToList(),
                DesiredQualities = qualityWeights,
                Decisions = SelectDecisions(qualityWeights).decision
            };
            concerns.Add(concern);     
        }

        reportingService.Writeline();
        reportingService.Writeline("Concerns");

        foreach (var concern in concerns)
        {
            reportingService.Writeline($"Concern {concerns.IndexOf(concern)}\n");
            reportingService.Writeline(concern.ToString());
            reportingService.Writeline();
        }

        return concerns;
    }

    public (List<Decision> decision, Dictionary<string, int> satisfactionScores) SelectDecisions(Dictionary<string, int> desiredQualities, OptimizerMode optimizerMode = OptimizerMode.ILP)
    {
        //Make weights a percentage
        var totalWeight = desiredQualities.Sum(kv => kv.Value);

        foreach (var kv in desiredQualities)
        {
            desiredQualities[kv.Key] = kv.Value * 100 / totalWeight;
        }

        Console.WriteLine("Finding optimal solution ...");

        Optimizer optimizer = new();
        var solution = optimizer.Optimize(optimizerMode, desiredQualities.Keys.ToList(), qualityArchPatternmatrix, desiredQualities);

        reportingService.Writeline();
        reportingService.Writeline("Optimal Solution");

        if (solution.decision.Any())
        {
            reportingService.Writeline($"Optimal Solution Found! Overall Score (out of 100):{solution.satisfactionScores.Average(s => s.Value)}");
            foreach (var score in solution.satisfactionScores.Where(kv => kv.Value != 0))
            {
                reportingService.Writeline($"{score.Key}: {score.Value}, (weight: {desiredQualities[score.Key]})");
            }
        }
        else
        {
            reportingService.Writeline("No optimal Solution Found!");
        }

        return solution;
    }

    private async Task ConsolidateSimilarConditions()
    {
        Console.WriteLine("Analyzing conditions ...");

        var gptService = new GptService();
        var instructions = "If the following conditions could mean the same thing or one can infer another or one can be considered a subset or another, return 'True' otherwise return 'False'. Just return True of False.";

        foreach (var req in requirements)
        {
            if (!conditionGroups.Any())
            {
                var newGroup = new ConditionGroup { NominalCondition = req.ConditionText };
                newGroup.Requirements.Add(req);
                conditionGroups.Add(newGroup);
            }
            else
            {
                var equivalentGroupFound = false;

                foreach (var group in conditionGroups)
                {
                    var ask = $"Condition 1: '{req.ConditionText}'\n " +
                        $"Condition 2: '{group.NominalCondition}'";

                    var response = await gptService.Call(instructions, ask);

                    bool equivalent = response.ToLower().Contains("true");

                    if (equivalent)
                    {
                        equivalentGroupFound = true;
                        group.Requirements.Add(req);
                    }
                }

                if (!equivalentGroupFound)
                {
                    var newGroup = new ConditionGroup { NominalCondition = req.ConditionText };
                    newGroup.Requirements.Add(req);
                    conditionGroups.Add(newGroup);
                }
            }
        }

        reportingService.Writeline();
        reportingService.Writeline("Condition Groups:");
        reportingService.Writeline(JsonConvert.SerializeObject(conditionGroups));
    }

    private async Task GenerateSatifiableGroups()
    {
        var gptService = new GptService();
        var instructions = "Task: Organize a provided set of conditions into distinct, non-contradictory groups. Once grouped, simply return the IDs of the conditions in each group enclosed in parentheses. For instance, if there are two groups where the first group includes requirements 1 and 3, and the second group includes requirements 3, your response should be formatted as ((1,2),(3)). Where the number indicate the id of the condition. Don't include the condition itself.\n" +
            "1. It is possible that one condition is part of more than one group" +
            "2. If a condition is applicable 'under any circumstances' or alway true include it in all groups";

        var ask = $"Conditions: {JsonConvert.SerializeObject(conditionGroups.Select(cg => cg.NominalCondition))}";
        var response = await gptService.Call(instructions, ask);
        ParseConditionResponse(response);
    }

    private void ParseConditionResponse(string response)
    {
        var result = new List<List<int>>();

        // Remove the outermost parentheses and split the string
        response = response.Trim('(', ')');
        var groups = Regex.Split(response, @"\)\s*,\s*\(");

        foreach (var group in groups)
        {
            var ids = new List<int>();
            var idStrings = group.Split(',');

            foreach (var idString in idStrings)
            {
                if (int.TryParse(idString.Trim(), out int id))
                {
                    ids.Add(id);
                }
                else
                {
                    throw new FormatException("Invalid format for ID");
                }
            }

            result.Add(ids);
        }

        for (int i = 0; i < result.Count; i++)
        {
            var satisfiableGroup = new SatisfiableGroup();

            foreach (var cgIndex in result[i])
            {
                satisfiableGroup.ConditionGroups.Add(conditionGroups.ElementAt(cgIndex - 1));
            }

            satisfiableGroups.Add(satisfiableGroup);
        }

        reportingService.Writeline();
        reportingService.Writeline("- Satisfiables Groups:");
        reportingService.Writeline(JsonConvert.SerializeObject(satisfiableGroups));
    }
}

