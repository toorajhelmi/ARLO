using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Research.DiscArch.Models;
using Research.DiscArch.Services;
namespace Research.DiscArch.ReqParsing;

public class MetricTriggerData
{
    public int Id { get; set; }
    public List<MetricTrigger> Triggers { get; set; }
}

public class RequirementParser
{
    private const int batchSize = 10;
    public const string AnyCircumstancesCondition = "under any circumstances";

    private IReportingService reportingService; 
    public List<Requirement> Requirements { get; set; } = new();

    public RequirementParser(IReportingService reportingService)
    {
        this.reportingService = reportingService;
    }

    public void LoadFromText(string text)
    {
        Requirements.Clear();
        string[] lines = text.Split('\n');

        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                Requirements.Add(new Requirement { Description = line });
            }
        }
    }

    public async Task Parse(ExperimentSettings experimentSettings)
    {
        Console.WriteLine(">> Parsing Reqs ...");

        var parsedFileName = $"{Enum.GetName<SystemNames>(experimentSettings.System)}-";

        parsedFileName += (experimentSettings.OnlySelectAbsolutelySignficant ? "stringent" : "lax") + ".json";

        if (experimentSettings.LoadReqsFromFile && File.Exists(parsedFileName))
        {
            Requirements.Clear();
            Requirements.AddRange(JsonConvert.DeserializeObject<List<Requirement>>(File.ReadAllText(parsedFileName)));
            ReportParsingStats(0);
            return;
        }

        if (!Requirements.Any())
            return;

        //Requirements = Requirements.Take(10).ToList();

        Console.WriteLine($"Parsing {Requirements.Count} Requirements ...");

        var gptService = new GptService();
        var architecturalRequirements = new List<Requirement>();

        var instructions = "I have provided a set of software requirements. I want you to extract the following information and return a JSON array of the Requirement class provide below.\n";

            if (experimentSettings.OnlySelectAbsolutelySignficant)
        {
            instructions += "1.Whether it is architecturally-significant. A requirement is Architecturally-significant if it satisfies both of these conditions:\n" +
                "1. It explicitly states a key decision regarding high-level software architecture." +
                "2. It specifies one or more of following quality attributes regarding software architecture:\n";
        }
        else
        {
            instructions += "Whether it is architecturally-significant. Architecturally-significant means specifying one or more of following quality attributes regarding overall software architecture (is it not considered architecturally-significant if it is just about some aspect of the software not impacting its architecture):\n";
            
        }

        instructions += "-Performance Efficiency: Achieving high performance under economic resource utilization.\n" +
            "-Compatibility: Interoperability and co-existence​​.\n" +
            "-Usability: A user-friendly app with straightforward and elegant UX and UI.\n" +
            "-Reliability: Stability under different conditions​​.\n" +
            "-Security: Protecting data, preventing breaches​​.\n" +
            "-Maintainability: Easy to modify and improve​​​​.\n" +
            "-Portability: Adaptable to different environments​.\n" +
            "-Cost Efficiency: Keep the overall cost (including development, operations, and maintenance) as low as possible\n" +
            "2. Find the quality attributes mentioned from the list above. (do not include anything outside of the above list.)\n" +
            "3. The ConditionText is a conditional statement provided in the requirement that should be true when the quality attributes are expected, e.g., 'if bandwidth is low', or 'when traffic is high' or 'under normal conditions' or 'all the time'. (if there is no condition return N/A\n" +
            "public class Requirement { public int Id { get; set; } public bool IsArchitecturallySignificant { get; set; } public List<string> QualityAttributes { get; set; } = new(); public string ConditionText { get; set; }}";

        int index = 1;

        var stopWatch = Stopwatch.StartNew();
        stopWatch.Start();

        while (index <= Requirements.Count)
        {
            var reqs = new StringBuilder();

            Console.WriteLine($"Parsing reqs {index} to {index + batchSize - 1} ...");
            foreach (var requirement in Requirements.Skip(index - 1).Take(batchSize))
            {
                //It the requirement is too long we only take 500 chars to avoid getting over the GPT token limits
                reqs.AppendLine(requirement.Description.Substring(0, int.Min(500, requirement.Description.Length)));
                index++;
            }

            try
            {
                var response = await gptService.Call(instructions, reqs.ToString());
                var parsedReqs = JsonConvert.DeserializeObject<List<Requirement>>(response);
                foreach (var parsedReq in parsedReqs)
                {
                    var requirement = Requirements.First(r => r.Id == parsedReq.Id);
                    requirement.ConditionText = parsedReq.ConditionText;
                    requirement.IsArchitecturallySignificant = parsedReq.IsArchitecturallySignificant;
                    requirement.QualityAttributes = parsedReq.QualityAttributes;
                    requirement.Parsed = true;

                    if (requirement.ConditionText == "" || requirement.ConditionText == "N/A")
                        requirement.ConditionText = AnyCircumstancesCondition;
                }
                //await ParseConditions();

                Console.WriteLine($"--> ASR Count (so far): {Requirements.Count(r => r.IsArchitecturallySignificant)}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"!!! ERROR Pasing Req !!!\n{e.Message}");
            }
        }

        stopWatch.Stop();
        ReportParsingStats(stopWatch.ElapsedMilliseconds);

        File.WriteAllText(parsedFileName, JsonConvert.SerializeObject(Requirements));
    }

    private void ReportParsingStats(long parsingTime)
    {
        reportingService.RecordStat("# Requirements", Requirements.Count);
        reportingService.RecordStat("Requirements with conditions", Requirements.Count(r => r.ConditionText != AnyCircumstancesCondition));
        reportingService.RecordStat("# ASR", Requirements.Count(r => r.IsArchitecturallySignificant));
        reportingService.RecordStat("Parsing Requirements", parsingTime);
        reportingService.WriteStats();
    }

    private async Task ParseConditions()
    {
        Console.WriteLine("Parsing conditions ...");

        var gptService = new GptService();

        var metricInstruction = "Extract list of metrics for each condition in the provided list (e.g., 'user numbers', 'bandwidth') and the value or quality specified for them ((e.g., 'increases significantly', 'below 50MB/S')\nReturn: Just return a json array of MetricTriggerData where MetricTriggerData is defined as below:\n public class MetricTriggerData { public int Id { get; set; } public List<MetricTrigger> Triggers { get; set; }}\n\npublic class MetricTrigger { public string Metric { get; set; } public string Trigger { get; set; }}";

        var ask = string.Join('\n', Requirements
            .Where(r => r.ConditionText != null)
            .Select(r => $"{r.Id}: {r.ConditionText}"));

        var response = "";

        try
        {
            ask = $"Conditions:\n {ask}";
            response = await gptService.Call(metricInstruction, ask);

            if (!response.StartsWith('['))
                response = $"[{response}]";
            //Console.WriteLine("Conditions response:\n" + response);
            var metricTriggerData = JsonConvert.DeserializeObject<List<MetricTriggerData>>(response);

            foreach (var metricTriggerList in metricTriggerData)
            {
                var matchingReq = Requirements.FirstOrDefault(r => r.Id == metricTriggerList.Id);
                if (matchingReq != null)
                    matchingReq.MetricTriggers = metricTriggerList.Triggers;
                else
                    Console.WriteLine("!!! ERROR !!!: " + $"No matching req for id{metricTriggerList.Id}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"!!! ERROR Parsing Condition !!!:\n{e.Message}\n{response}");
        }
    }
}

// OLD PARSING CODE
//    foreach (var reqDetails in requirementDetails)
//    {
//        try
//        {
//            var idString = reqDetails.Split('.')[0].Trim('R', ':', '.').Trim();
//            var id = int.Parse(idString);
//            var req = Requirements.First(r => r.Id == id);
//            req.Parsed = true;

//            if (reqDetails.Contains("Significant: 'Yes'", StringComparison.InvariantCultureIgnoreCase) ||
//                reqDetails.Contains("Significant: Yes", StringComparison.InvariantCultureIgnoreCase))
//            {
//                req.IsArchitecturallySignificant = true;
//                string[] delimiters = { "Quality: ", "Condition: ", "\n" };
//                var parts = reqDetails.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

//                if (parts.Length > 1)
//                {
//                    var qualityAttributes = parts[1].Trim('\n').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
//                                                   .Select(qa => qa.Trim())
//                                                   .ToList();
//                    req.QualityAttributes = qualityAttributes;
//                }

//                req.ConditionText = parts.Length > 2 ? parts[2].Trim('\n').Trim() : string.Empty;
//                if (req.ConditionText.ToUpper() == "N/A" || req.ConditionText.ToUpper() == "NONE")
//                {
//                    req.ConditionText = string.Empty;
//                }
//            }
//            else
//            {
//                req.IsArchitecturallySignificant = false;
//            }
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e.Message);
//        }
//    }

//    await ParseConditions();
//}