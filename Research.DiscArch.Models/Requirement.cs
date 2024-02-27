namespace Research.DiscArch.Models;

public class MetricTrigger
{
    public string Metric { get; set; }
    public string Trigger { get; set; }

    public override string ToString()
    {
        return $"{Metric}: {Trigger}";
    }
}

public class Requirement
{
    static int id = 0;

    public Requirement()
    {
        id++;
        Id = id;
    }
    
    public int Id { get; set; }
    public bool Parsed { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public bool IsArchitecturallySignificant { get; set; }
    public bool IsNFR { get; set; }
    public List<string> QualityAttributes { get; set; } = new();
    public string ConditionText { get; set; }
    //public string ConditionType { get; set; }
    public Condition Condition { get; set; }
    public List<MetricTrigger> MetricTriggers { get; set; } = new();
}