using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Research.DiscArch.Models;
using Research.DiscArch.Services;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Designer;

public class Architect
{
    private static Matrix qualityArchPatternmatrix = new Matrix();

    static Architect()
    {
        qualityArchPatternmatrix = ResourceManager.LoadArchPattenMatrix();
    }

    public Architect()
    {
    }

    public async Task<IEnumerable<Concern>> SelectArch(List<Requirement> requirements)
    {
        Console.WriteLine("Analyzing conditions ...");

        var gptService = new GptService();
        var instructions = "Task: Organize a provided set of conditions into distinct, non-contradictory groups. Once grouped, simply return the IDs of the conditions in each group enclosed in parentheses. For instance, if there are two groups where the first group includes requirements 1 and 3, and the second group includes requirements 3, your response should be formatted as ((1,2),(3)).\n" +
            "1. It is possible that one condition is part of more than one group" +
            "2. If a condition is applicable 'under any circumstances' or alway true include it in all groups";

        var conditions = requirements.Where(r => !string.IsNullOrEmpty(r.ConditionText)).Select(r => new
        {
            Id = requirements.IndexOf(r),
            Condition = r.ConditionText
        });

        Console.WriteLine("- Conditions:");
        Console.WriteLine(JsonConvert.SerializeObject(conditions));

        var ask = $"Conditions: {JsonConvert.SerializeObject(conditions)}";
        var response = await gptService.Call(instructions, ask);
        var groups = ParseConditionResponse(response);

        Console.WriteLine();
        Console.WriteLine("- Uncontradictory Groups:");
        Console.WriteLine(response);

        var concerns = new List<Concern>();

        foreach (var ug in groups)
        {
            var qualityAttrbutes = ug.SelectMany(g => requirements[g].QualityAttributes).Distinct().ToList();

            var concern = new Concern
            {
                Conditions = ug.Select(g => requirements[g].ConditionText).ToList(),
                DesiredQualities = qualityAttrbutes,
                Decisions = SelectDecisions(qualityAttrbutes)
            };
            concerns.Add(concern);
        }

        return concerns;
    }

    public static List<Decision> SelectDecisions(List<string> desiredQualities)
    {
        var decisions = new List<Decision>();

        foreach (var group in qualityArchPatternmatrix.RowGroups.GroupBy(kv => kv.Value).Select(g => g.Key))
        {
            var decision = new Decision { ArchPatternName = group, Score = int.MinValue };
            decisions.Add(decision);

            foreach (var row in qualityArchPatternmatrix.GetRowsByGroup(group))
            {
                var satisfiedQualities = new List<(string, int)>();
                var unsatisfiedQualities = new List<(string, int)>();
                int rowValue = 0;

                foreach (var column in row.Value)
                {
                    if (desiredQualities.Contains(column.Key))
                    {
                        rowValue += column.Value;
                        if (column.Value > 0)
                            satisfiedQualities.Add((column.Key, column.Value));
                        else if (column.Value < 0)
                            unsatisfiedQualities.Add((column.Key, column.Value));
                    }
                }

                if (rowValue > decision.Score)
                {
                    decision.Score = rowValue;
                    decision.SelectedPattern = row.Key;
                    decision.SatisfiedQualties = satisfiedQualities;
                    decision.UnsatisfiedQualties = unsatisfiedQualities;
                }
            }
        }     

        return decisions;
    }

    private List<List<int>> ParseConditionResponse(string response)
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

        return result;
    }

}

