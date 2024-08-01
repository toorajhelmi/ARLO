using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Research.DiscArch.Models;
using Research.DiscArch.ReqParsing;
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

public class CustomPoint : ICloneable
{
    public double[] Values { get; set; }

    public CustomPoint(double[] values)
    {
        Values = values;
    }

    public object Clone()
    {
        return new CustomPoint(Values);
    }
}

public class Architect
{
    private Matrix qualityArchPatternmatrix = new();
    private IReportingService reportingService;
    private ExperimentSettings experimentSettings;
    private List<Requirement> requirements;
    private List<ConditionGroup> conditionGroups = new();

    public List<Concern> Concerns = new();
    public List<SatisfiableGroup> SatisfiableGroups = new();
    public Dictionary<string, int> QualityWeights = new();

    public Architect(IReportingService reportingService, ExperimentSettings experimentSettings, List<Requirement> requirements)
    {
        this.reportingService = reportingService;
        this.experimentSettings = experimentSettings;
        this.requirements = requirements;

        qualityArchPatternmatrix = ResourceManager.LoadArchPattenMatrix(experimentSettings.MatrixPath);

        if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.EquallyImportant))
        {
            foreach (var column in qualityArchPatternmatrix.GetRows().First().Value)
            {
                QualityWeights[column.Key] = 1;
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
                    if (!QualityWeights.ContainsKey(quality))
                        QualityWeights[quality] = 0;
                    QualityWeights[quality]++;
                }
            }
        }
        else if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.Inferred))
        {
            //This will be handled within the optimizer for each group
        }
        else if (experimentSettings.QualityWeightsMode == Enum.GetName(QualityWeightsMode.Provided))
        {
            QualityWeights = experimentSettings.ProvidedQualityWeights;
        }
    }

    public async Task<IEnumerable<Concern>> SelectArch(bool reportResults = true, SatisfiableGroup specificSatisfiabilityGroup = null)
    {
        var cgFile = $"{Enum.GetName(experimentSettings.System)}-";

        cgFile += (experimentSettings.OnlySelectAbsolutelySignficant ? "stringent" : "lax") + "-cg.json";
        var stopWatch = Stopwatch.StartNew();

        if (!experimentSettings.JustRunOptimization)
        {
            Concerns.Clear();
;
            if (experimentSettings.LoadConditionGroupsFromFile && File.Exists(cgFile))
            {
                conditionGroups = JsonConvert.DeserializeObject<List<ConditionGroup>>(File.ReadAllText(cgFile));
            }
            else
            {
                stopWatch.Start();
                await GenerateConditionGroups();
                stopWatch.Stop();
                reportingService.RecordStat("Condition Groups Count", conditionGroups.Count);
                reportingService.RecordStat("Extracting condition groups", stopWatch.ElapsedMilliseconds);
            }

            stopWatch.Reset();
            stopWatch.Start();
            await GenerateSatifiableGroups();
            stopWatch.Stop();
            reportingService.RecordStat("Satisfiable Groups Count", SatisfiableGroups.Count);
            reportingService.RecordStat("Extracting satisfiable groups", stopWatch.ElapsedMilliseconds);

            if (experimentSettings.UseOnlyFirstSatisfiableGroup && SatisfiableGroups.Any())
            {
                SatisfiableGroups = new List<SatisfiableGroup> { SatisfiableGroups.First() };
            }

            foreach (var sg in SatisfiableGroups)
            {
                QualityWeights.Clear();

                stopWatch.Reset();
                stopWatch.Start();

                var qualityAttrbutes = sg.ConditionGroups.SelectMany(cg => cg.Requirements.SelectMany(r => r.QualityAttributes)).ToList();

                foreach (var quality in qualityAttrbutes)
                {
                    if (!QualityWeights.ContainsKey(quality))
                        QualityWeights[quality] = 0;
                    QualityWeights[quality]++;
                }

                var concern = new Concern
                {
                    SatisfiableGroup = sg,
                    DesiredQualities = QualityWeights.ToDictionary(entry => entry.Key, entry => entry.Value),
                    Decisions = SelectDecisions(QualityWeights).decision
                };
                Concerns.Add(concern);

                stopWatch.Stop();
            }

            reportingService.Writeline();
            reportingService.Writeline("Concerns");

            foreach (var concern in Concerns)
            {
                reportingService.Writeline();
                reportingService.Writeline("===========================================================", Verbosity.Results);
                reportingService.Writeline($"Concern {Concerns.IndexOf(concern) + 1}\n", Verbosity.Results);
                reportingService.Writeline(concern.ToString(), Verbosity.Results);
                reportingService.Writeline();
            }

            return Concerns;
        }
        else
        {
            var concern = new Concern
            {
                SatisfiableGroup = specificSatisfiabilityGroup,
                DesiredQualities = experimentSettings.ProvidedQualityWeights,
                Decisions = SelectDecisions(experimentSettings.ProvidedQualityWeights).decision
            };

            if (reportResults)
            {
                reportingService.Writeline();
                reportingService.Writeline("Concerns");

                reportingService.Writeline($"Concern {Concerns.IndexOf(concern)}\n", Verbosity.Results);
                reportingService.Writeline(concern.ToString(), Verbosity.Results);
                reportingService.Writeline();
            }

            return new List<Concern> { concern };
        }
    }

    public (List<Decision> decision, Dictionary<string, int> satisfactionScores) SelectDecisions(Dictionary<string, int> desiredQualities, OptimizerMode optimizerMode = OptimizerMode.ILP)
    {
        var normalizedweights = new Dictionary<string, int>();

        var totalWeight = desiredQualities.Sum(kv => kv.Value);

        foreach (var kv in desiredQualities)
        {
            normalizedweights[kv.Key] = kv.Value * 100 / totalWeight;
        }

        Optimizer optimizer = new();
        var solution = optimizer.Optimize(optimizerMode, normalizedweights.Keys.ToList(), qualityArchPatternmatrix, normalizedweights);

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

    private Dictionary<int, List<Requirement>> MapConditionsToClusters(List<Requirement> requirements, List<int> clusters)
    {
        var clusterMap = new Dictionary<int, List<Requirement>>();
        for (int i = 0; i < requirements.Count; i++)
        {
            if (!clusterMap.ContainsKey(clusters[i]))
            {
                clusterMap[clusters[i]] = new List<Requirement>();
            }
            clusterMap[clusters[i]].Add(requirements[i]);
        }
        return clusterMap;
    }

    private async Task GenerateConditionGroups()
    {
        Console.WriteLine(">> Generating Condition Groups ...");

        var stopWatch = Stopwatch.StartNew();

        var gptService = new GptService();
        
        var reqsWithConditions = requirements.Where(r => r.ConditionText != RequirementParser.AnyCircumstancesCondition).ToList();

        Console.WriteLine($"Generating condition groups for {reqsWithConditions.Count()} reqs with conditions ...");

        if (experimentSettings.GroupingStrategy == GroupingStrategy.Clustering)
        {
            var instructions = "If the following conditions could mean the same thing or one can infer another or one can be considered a subset or another, return 'True' otherwise return 'False'. Just return True of False.";

            var embeddings = await gptService.GetEmbeddings(reqsWithConditions.Select(r => r.ConditionText).ToList());

            for (int i = 0; i < reqsWithConditions.Count(); i++)
            {
                reqsWithConditions[i].ConditionEmbeddings = embeddings[i];
            }

            var clusteringService = new ClusteringService();

            stopWatch.Start();
            var clusters = clusteringService.ClusterConditions(embeddings);
            stopWatch.Stop();

            reportingService.RecordStat("Clustering Time", stopWatch.ElapsedMilliseconds);

            var clusterMap = MapConditionsToClusters(reqsWithConditions, clusters);
            reportingService.RecordStat("Cluster Count", clusterMap.Count());
            Console.WriteLine($"Formed {clusterMap.Count()} clusters");

            var conditionLessReqs = requirements.Where(r => r.ConditionText == RequirementParser.AnyCircumstancesCondition);
            if (conditionLessReqs.Any())
            {
                var newGroup = new ConditionGroup { NominalCondition = conditionLessReqs.First().ConditionText };
                newGroup.Requirements.AddRange(conditionLessReqs);
                conditionGroups.Add(newGroup);
            }

            int clusterIndex = 0;

            foreach (var clusteredReqs in clusterMap)
            {
                Console.WriteLine($"Finding simliar conditions within cluster {++clusterIndex} with {clusteredReqs.Value.Count} reqs");

                var clusterGroups = new List<ConditionGroup>();

                foreach (var req in clusteredReqs.Value)
                {
                    Console.Write(".");

                    if (!clusterGroups.Any())
                    {
                        var newGroup = new ConditionGroup { NominalCondition = req.ConditionText };
                        newGroup.Requirements.Add(req);
                        clusterGroups.Add(newGroup);

                        Console.WriteLine($"Adding {clusterGroups.Count} condition group");
                    }
                    else
                    {
                        var equivalentGroupFound = false;

                        foreach (var group in clusterGroups)
                        {
                            var ask = $"Condition 1: '{req.ConditionText}'\n " +
                                $"Condition 2: '{group.NominalCondition}'";

                            var response = await gptService.Call(instructions, ask);

                            bool equivalent = response.ToLower().Contains("true");

                            if (equivalent)
                            {
                                equivalentGroupFound = true;
                                group.Requirements.Add(req);
                                break;
                            }
                        }

                        if (!equivalentGroupFound)
                        {
                            var newGroup = new ConditionGroup { NominalCondition = req.ConditionText };
                            newGroup.Requirements.Add(req);
                            clusterGroups.Add(newGroup);
                            Console.WriteLine($"Adding {clusterGroups.Count} condition group");
                        }
                    }
                }

                conditionGroups.AddRange(clusterGroups);
            }
        }
        else
        {
            //int condIndex = 0;
            //int words = 0;
            //while (words < GptService.MaxWordPerCall)
            //{
            //    if (reqsWithConditions[condIndex].ConditionWordCount == 0)
            //        reqsWithConditions[condIndex].ConditionWordCount = reqsWithConditions[condIndex].ConditionText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count();

            //    if (words + reqsWithConditions[condIndex].ConditionWordCount < GptService.MaxWordPerCall)
            //    {
            //        condIndex++;
            //        words += reqsWithConditions[condIndex].ConditionWordCount;
            //    }
            //}


        }

        var cgFile = $"{Enum.GetName(experimentSettings.System)}-";
        cgFile += (experimentSettings.OnlySelectAbsolutelySignficant ? "stringent" : "lax") + "-cg.json";

        File.WriteAllText(cgFile, JsonConvert.SerializeObject(conditionGroups));
        Console.WriteLine();
         
        reportingService.Writeline();
        reportingService.Writeline("Condition Groups:");
        reportingService.Writeline(string.Join('\n', conditionGroups.Select(cg => cg.NominalCondition)));
    }

    private async Task GenerateSatifiableGroups()
    {
        Console.WriteLine(">> Generating Satifiable Groups ...");

        var gptService = new GptService();
        var instructions = "Organize the provided set of conditions into groups where conditions in the same group can be true at the same time. Once grouped, simply return the IDs of the conditions in each group enclosed in parentheses. For instance, if there are two groups where the first group includes requirements 1 and 3, and the second group includes requirement 3 and 4, your response should be formatted as ((1,2),(3,4)), where each number indicates the id of the corresponding condition.\n" +
            "1. It is possible that one condition is part of more than one group\n" +
            "2. If a condition is called 'under any circumstances', include it in all groups\n" +
            "3. Just return the ID inside parantheses. Do not return the condition's text or any other text before or after the requested format.";

        var ask = $"Conditions:\n" + string.Join('\n', conditionGroups.Select(cg => conditionGroups.IndexOf(cg) + 1 + ":" + cg.NominalCondition));

        //This sometimes does not return as expected. Trying a few times to be safe.
        for (int attempts = 0; attempts < 3; attempts++)
        {
            try
            {
                var response = await gptService.Call(instructions, ask);
                ParseConditionResponse(response);
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
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

            SatisfiableGroups.Add(satisfiableGroup);
        }

        reportingService.Writeline();
        reportingService.Writeline("- Satisfiables Groups:");
        reportingService.Writeline(JsonConvert.SerializeObject(SatisfiableGroups));
    }
}

