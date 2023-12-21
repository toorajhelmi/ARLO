using System;
namespace Research.DiscArch.Models
{
	public class Concern
	{
		public List<string> Conditions { get; set; }
		public List<string> DesiredQualities { get; set; }
        public List<Decision> Decisions { get; set; }
        public double OverallScore => 100 * Decisions.Average(d => d.Score) / DesiredQualities.Count();

        public override string ToString()
        {
            return $"Conditions:\n{string.Join('\n', Conditions)}\n\nDesired Qualities:{string.Join(',', DesiredQualities)}\n\nOverall Score (Max 100): {OverallScore}\n\nDecisions:\n{string.Join('\n', Decisions)}";
        }
    }
}

