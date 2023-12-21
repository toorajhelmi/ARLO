namespace Research.DiscArch.Models
{
    public class Decision
	{
		public string ArchPatternName { get; set; }
		public string SelectedPattern { get; set; }
		public int Score { get; set; }
		public List<(string quality, int score)> SatisfiedQualties { get; set; } = new();
		public List<(string quality, int score)> UnsatisfiedQualties { get; set; } = new();

        public override string ToString()
        {
			if (SatisfiedQualties.Count + UnsatisfiedQualties.Count == 0)
				return $"{SelectedPattern} selected for {ArchPatternName} without impacting any qualities."; 
			var description = $"{SelectedPattern} selected for {ArchPatternName}.";
			if (SatisfiedQualties.Any())
				description += $"\n- satisfying\n {string.Join('\n', SatisfiedQualties.Select(sq => $"-- {sq.quality}: {sq.score}"))}";
			if (UnsatisfiedQualties.Any())
                description += $"\n- Not satisfying\n {string.Join('\n', UnsatisfiedQualties.Select(sq => $"-- { sq.quality}: { sq.score}"))}";
            return description;
        }
    }
}

