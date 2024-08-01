using Research.DiscArch.Designer;
using Research.DiscArch.Models;
using Research.DiscArch.Services;

namespace Research.DiscArch.Console
{
    public class IdentifyingInflunetialSetsExperiment : RequirementBasedExperiment
    {
        public List<(List<(Decision Original, Decision New)> ChangedDecisions, List<Requirement> Requirements)> InfluentialSets = new();

        public IdentifyingInflunetialSetsExperiment(ExperimentSettings experimentSettings)
            : base(experimentSettings)
        {
            experimentSettings.ExperimentKind = ExperimentKind.Varying;
        }

        public async void Run()
        {
            experimentSettings.OnlySelectAbsolutelySignficant = true;

            await LoadRequirements();

            var architect = new Architect(reportingService, experimentSettings, asrs);
            await architect.SelectArch();

            foreach (var concern in architect.Concerns)
            {
                experimentSettings.JustRunOptimization = true;

                var attributeSensitvities = await PerformQASensitivityAnalysis(architect, concern);

                architect.QualityWeights.ToList().ForEach(item => experimentSettings.ProvidedQualityWeights[item.Key] = item.Value);

                var satisiableReqs = concern.SatisfiableGroup.ConditionGroups.SelectMany(cg => cg.Requirements).ToList();
                //var satisiableReqs = concern.Requirements;

                var influentialSet = new List<Requirement>();

                while (attributeSensitvities.Any())
                {
                    var sensitiveQA = attributeSensitvities.Aggregate((l, r) => l.Value > r.Value ? l : r);
                    attributeSensitvities.Remove(sensitiveQA.Key);

                    //This is when we have removed all reqs so there no point running ARLO anymore.
                    if (!attributeSensitvities.Any())
                        break;

                    var contributingReqs = satisiableReqs.Where(r => r.QualityAttributes.Any(qa => qa == sensitiveQA.Key)).ToList();

                    foreach (var contributingReq in contributingReqs)
                    {
                        foreach (var qa in contributingReq.QualityAttributes)
                        {
                            experimentSettings.ProvidedQualityWeights[qa]--;
                        }

                        influentialSet.Add(contributingReq);
                        satisiableReqs.Remove(contributingReq);

                        //contributingReqs.RemoveAt(0);
                        //architect = new Architect(reportingService, experimentSettings, asrs);
                        //Note: This is not 100% accurate as I am assuming satisfiability groups remain the same after removing a req.
                        //However, otherwsie, we need to rerun ARLO which is costly.
                        var newDecisions = (await architect.SelectArch(false, concern.SatisfiableGroup)).First().Decisions;
                        var changedDecisions = new List<(Decision Original, Decision New)>();

                        foreach (var od in concern.Decisions)
                        {
                            var newDecision = newDecisions.First(d => d.ArchPatternName == od.ArchPatternName);

                            if (od.SelectedPattern != newDecision.SelectedPattern)
                            {
                                var changedDecision = new Tuple<Decision, Decision>(od, newDecision);

                                changedDecisions.Add((od, newDecision));
                            }
                        }

                        if (changedDecisions.Any())
                        {
                            InfluentialSets.Add((changedDecisions, influentialSet.ToList()));
                            influentialSet.Clear();
                            architect.QualityWeights.ToList().ForEach(item => experimentSettings.ProvidedQualityWeights[item.Key] = item.Value);
                        }
                    }
                }

                reportingService.Writeline();
                reportingService.Writeline("|||||||||||||||||||||||||||||||||||||||||||||||", Verbosity.Results);
                reportingService.Writeline($"||||| Analyzing Satisfiability Group {architect.Concerns.IndexOf(concern) + 1} ||||||", Verbosity.Results);

                if (!InfluentialSets.Any())
                {
                    reportingService.Writeline($"No Influential Set Found!", Verbosity.Results);
                }

                foreach (var (ChangedDecisions, Requirements) in InfluentialSets)
                {
                    reportingService.Writeline("===================================================", Services.Verbosity.Results);
                    reportingService.Writeline("    Influential Set Found", Services.Verbosity.Results);
                    reportingService.Writeline("===================================================", Services.Verbosity.Results);
                    reportingService.Writeline(string.Join('\n', ChangedDecisions.Select(cd => $"{cd.Original.ArchPatternName}: {cd.Original.SelectedPattern} -> {cd.New.SelectedPattern}")), Services.Verbosity.Results);
                    reportingService.Writeline("--------------------------------------------------", Services.Verbosity.Results);

                    foreach (var req in Requirements)
                    {
                        reportingService.Writeline(req.ToShortString(), Verbosity.Results);
                    }

                    reportingService.Writeline("--------------------------------------------------", Services.Verbosity.Results);
                }
            }

            var groupCounts = InfluentialSets
             .GroupBy(set => set.Requirements.Count)
             .Select(group => new { RequirementCount = group.Key, GroupCount = group.Count() })
             .ToList();

            foreach (var set in groupCounts)
            {
                reportingService.RecordStat($"AIS of {set.RequirementCount}", set.GroupCount);
            }

            reportingService.WriteStats();
            System.Console.WriteLine("Done!");
            System.Console.ReadLine();
        }

        private async Task<Dictionary<string, double>> PerformQASensitivityAnalysis(Architect architect, Concern concern)
        {
            var delta = architect.QualityWeights.Max(qw => qw.Value) * 1;
            var senstivity = new Dictionary<string, double>();

            experimentSettings.JustRunOptimization = true;
            experimentSettings.UseOnlyFirstSatisfiableGroup = true;

            architect.QualityWeights.ToList().ForEach(item => experimentSettings.ProvidedQualityWeights[item.Key] = item.Value);

            //var originalScore = architect.Concerns.First().TotalScore;
            //var originalDecisions = new List<Decision>((await architect.SelectArch(false, satisfiableGroup)).First().Decisions);

            foreach (var qw in architect.QualityWeights)
            {
                experimentSettings.ProvidedQualityWeights[qw.Key] -= delta;

                if (experimentSettings.ProvidedQualityWeights[qw.Key] < 0)
                    experimentSettings.ProvidedQualityWeights[qw.Key] = 0;

                await architect.SelectArch(false, concern.SatisfiableGroup);

                experimentSettings.ProvidedQualityWeights[qw.Key] += delta;

                //var positiveDecision = architect.Concerns.First().TotalScore;
                var newDecisions = new List<Decision>((await architect.SelectArch(false, concern.SatisfiableGroup)).First().Decisions);

                senstivity[qw.Key] = concern.Decisions.Count(od => newDecisions.First(pd => pd.ArchPatternName == od.ArchPatternName).SelectedPattern != od.SelectedPattern);
            }

            return senstivity;
        }
    }
}

