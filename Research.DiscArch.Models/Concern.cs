namespace Research.DiscArch.Models
{
    public class Concern
	{
		public List<string> Conditions { get; set; }
		public Dictionary<string, int> DesiredQualities { get; set; }
        public List<Decision> Decisions { get; set; }
        public double AverageScore => Decisions.Average(d => d.Score);

        public override string ToString()
        {
            return $"Conditions:\n{string.Join('\n', Conditions)}\n\nDesired Qualities:{string.Join(',', DesiredQualities)}\n\nAverage Decision Score (Max 100): {AverageScore}\n\nDecisions:\n{string.Join('\n', Decisions)}";
        }
    }
}

