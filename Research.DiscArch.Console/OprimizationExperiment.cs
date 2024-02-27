
using Research.DiscArch.Designer;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Console
{
	public class OprimizationExperiment
	{
		public void Run()
		{
            var qualityArchPatternmatrix = ResourceManager.LoadArchPattenMatrix();
			var optimizer = new Optimizer();
			var group1Results = optimizer.Optimize(OptimizerMode.ILP, new List<string> { "Performance Efficiency", "Security" }, qualityArchPatternmatrix, new Dictionary<string, int> { { "Performance Efficiency", 1 }, { "Security", 1} });
            var group2Results = optimizer.Optimize(OptimizerMode.ILP, new List<string> { "Performance Efficiency", "Reliability", "Usability" }, qualityArchPatternmatrix, new Dictionary<string, int> { { "Performance Efficiency", 1 }, { "Reliability", 4 },{ "Usability", 1 } });
            System.Console.WriteLine(group1Results.decision.ToString());
        }
	}
}

