namespace Research.DiscArch.Models
{
    public class Condition
	{
        public string Text { get; set; }
        public string ConditionType { get; set; }

        public override string ToString() => Text;
    }
}

