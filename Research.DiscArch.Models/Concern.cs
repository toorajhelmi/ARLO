namespace Research.DiscArch.Models
{
    public class Concern
	{
        public Dictionary<string, int> DesiredQualities { get; set; } = new();
        public List<Decision> Decisions { get; set; }
        public SatisfiableGroup SatisfiableGroup { get; set; }
        public List<string> Conditions => SatisfiableGroup.ConditionGroups.Select(c => c.NominalCondition).ToList();
        //public List<Requirement> Requirements => SatisfiableGroup.ConditionGroups.SelectMany(cg => Requirements).ToList();
        public double AverageScore => Decisions.Average(d => d.Score);
        public int TotalScore => Decisions.Sum(d => d.Score);

        public override string ToString()
        {
            return $"Conditions:\n{string.Join('\n', Conditions)}\n\nDesired Qualities:{string.Join(',', DesiredQualities.OrderByDescending(entry => entry.Value).ToDictionary(entry => entry.Key, entry => entry.Value))}\n\nAverage Decision Score (Max 100): {AverageScore}\n\nDecisions:\n{string.Join('\n', Decisions)}";
        }
    }
}

