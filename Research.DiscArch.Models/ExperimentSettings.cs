namespace Research.DiscArch.Models
{
    public class ExperimentSettings
	{
		public string SystemName { get; set; }
		public string OptimizationStrategy { get; set; }
		public string QualityWeightsMode { get; set; }
        public Dictionary<string, int> ProvidedQualityWeights { get; set; }
		public Matrix Matrix { get; set; }
		public bool JustRunOptimization { get; set; }
    }
}

