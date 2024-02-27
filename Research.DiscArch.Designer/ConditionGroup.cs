using System;
using Research.DiscArch.Models;

namespace Research.DiscArch.Designer
{
	public class ConditionGroup
	{
		public string NominalCondition { get; set; }
		public List<Requirement> Requirements { get; set; } = new();
	}
}

