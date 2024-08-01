namespace Research.DiscArch.Models
{
    public class ConditionGroup
	{
		public string NominalCondition { get; set; }
		public List<Requirement> Requirements { get; set; } = new();
	}
}

