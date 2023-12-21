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
    public List<Requirement> Requirements { get; set; } = new();

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

    public async Task Parse()
    {
        var gptService = new GptService();
        var architecturalRequirements = new List<Requirement>();

        var instructions = "I provide a set of software requirements. I want you to extract the information below and return a json arraty of Requirment class provide below.\n" +
            "1. Whether it is architecturally significant or not (Architecturally signficant means stating exlicity or implicity a statement about one or more of these quality attrbitues:\n" +
            "Efficiency: Economic resource utilization.\n" +
            "Compatibility: Interoperability and co - existence​​.\n" +
            "Usability: Usable by different users easily.\n" +
            "Reliability: Stability under different conditions​​.\n" +
            "Security: Protecting data, preventing breaches​​.\n" +
            "Maintainability: Easy to modify and improve​​​​.\n\n" +
            "Portability: Adaptable to different environments​.\n\n" +
            "2. Find the quality attributes mentinoed from the list above." +
            "3. The condition - if any - that should be satisfied when the quality attrbiutes are expected. Condition could be a condtional statement, e.g., 'if bandwidth is low' or stated as a system state e.g., 'under normal conditions' or 'all the time'\n" +
            "3.A. ConditionText: The conditon statement (if there is no condition return N/A\n" +
            "3.B. ConditionType: Specify whether the condition is a 'Level' (static value) or 'Rate' (dynamic change)\n\n" +
            "public class Requirement { public int Id { get; set; } public bool IsArchitecturallySignificant { get; set; } public List<string> QualityAttributes { get; set; } = new(); public string ConditionText { get; set; } public string ConditionType { get; set; }}";

        var reqs = new StringBuilder();
        int index = 1;

        foreach (var requirement in Requirements)
        {
            reqs.AppendLine($"{requirement.Id}: {requirement.Description}");
            index++;
        }

        var response = await gptService.Call(instructions, reqs.ToString());

        try
        {
            var parsedReqs = JsonConvert.DeserializeObject<List<Requirement>>(response);
            foreach (var parsedReq in parsedReqs)
            {
                var requirement = Requirements.First(r => r.Id == parsedReq.Id);
                requirement.ConditionText = parsedReq.ConditionText;
                requirement.ConditionType = parsedReq.ConditionType;
                requirement.IsArchitecturallySignificant = parsedReq.IsArchitecturallySignificant;
                requirement.QualityAttributes = parsedReq.QualityAttributes;
                requirement.Parsed = true;

                if (requirement.ConditionText == "" || requirement.ConditionText == "N/A")
                    requirement.ConditionText = "under any circumstances";
            }
            await ParseConditions();
        }
        catch (Exception e)
        {
            Console.WriteLine("!!! ERROR !!!: " + e.Message);
        }
    }

    private async Task ParseConditions()
    {
        var gptService = new GptService();

        var metricInstruction = "Extract list of metrics for each condition in the provided list (e.g., 'user numbers', 'bandwidth') and the value or quality specified for them ((e.g., 'increases significantly', 'below 50MB/S')\nReturn: Just return a json array of MetricTriggerData where MetricTriggerData is defined as below:\n public class MetricTriggerData { public int Id { get; set; } public List<MetricTrigger> Triggers { get; set; }}\n\npublic class MetricTrigger { public string Metric { get; set; } public string Trigger { get; set; }}";

        var ask = string.Join('\n', Requirements
            .Where(r => r.ConditionText != null)
            .Select(r => $"{r.Id}: {r.ConditionText}"));

        try
        {
            ask = $"Conditions:\n {ask}";
            var response = await gptService.Call(metricInstruction, ask);

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
            Console.WriteLine("!!! ERROR !!!: " + e.Message);
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