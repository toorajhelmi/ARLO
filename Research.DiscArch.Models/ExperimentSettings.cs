namespace Research.DiscArch.Models
{
    public enum ExperimentKind
    {
        BasicArlo,
        Varying,
        Optimiztion
    }

    public enum SystemNames
    {
        Messaging,
        Kaggle,
        Contradictory,
        AppFlowy,
        MLonBL,
        SmallMessagingSystem,
        OfficerDispatcher,
        Bamboo,
        Mule,
        Aptana,
        SpringXd
    }

    public enum GroupingStrategy
    {
        Clustering,
        DC
    }

    public class RemovalStrategy
    {
        public enum RemovalKind
        {
            Even,
            Targeted
        }

        public RemovalKind Kind { get; set; }
        public List<string> DesiredQAs { get; set; } = new();
        public List<string> UndesiredQAs { get; set; } = new();
    }

    public class ExperimentSettings
	{
        public bool OnlySelectAbsolutelySignficant { get; set; }
		public string OptimizationStrategy { get; set; }
		public string QualityWeightsMode { get; set; }
        public Dictionary<string, int> ProvidedQualityWeights { get; set; } = new();
		public Matrix Matrix { get; set; }
		public bool JustRunOptimization { get; set; }
        public bool LoadReqsFromFile { get; set; }
        public bool LoadConditionGroupsFromFile { get; set; }
        public SystemNames System { get; set; }
        public ExperimentKind ExperimentKind { get; set; }
        public string ReportsTag { get; set; } = "";
        public GroupingStrategy GroupingStrategy { get; set; }
        public RemovalStrategy RemovalStratgy { get; set; } = new();
        public bool UseOnlyFirstSatisfiableGroup { get; set; }
        public string MatrixPath { get; set; }

        public string ExpermentName => $"{Enum.GetName(ExperimentKind)}-{Enum.GetName(System)}";


    }
}

