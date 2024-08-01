using Accord.MachineLearning;
using Accord.Math.Distances;

namespace Research.DiscArch.Services
{
    public class CustomPoint : ICloneable
    {
        public double[] Values { get; set; }

        public CustomPoint(double[] values)
        {
            Values = values;
        }

        public object Clone()
        {
            return new CustomPoint(Values);
        }
    }

    public class ClusteringService
    {
        public List<int> ClusterConditions(List<List<double>> embeddings, int maxClusters = 20, int maxClusterSize = 30)
        {
            var clusterData = embeddings.Select(e => e.ToArray()).ToArray();
            var clustersScoresList = new List<double>();

            // For a small list (the number of items in the list is less than maxClusters), we only consider up to half the number of items
            for (int k = embeddings.Count / 10; k <= embeddings.Count / 5; k++)
            {
                var kmeans = new KMeans(k, new SquareEuclidean());
                var clusters = kmeans.Learn(clusterData);
                var labels = clusters.Decide(clusterData);
                double score = CalculateWCSS(clusterData, clusters.Centroids, labels);
                clustersScoresList.Add(score);
            }

            int optimalK = embeddings.Count / 10 + FindElbowPoint(clustersScoresList);
            var finalKMeans = new KMeans(optimalK, new SquareEuclidean());
            var finalClusters = finalKMeans.Learn(clusterData);
            var clusterAssignments = finalClusters.Decide(clusterData);

            var clusterGroups = clusterAssignments.GroupBy(c => c).ToList();
            var adjustedAssignments = new List<int>();
            int currentClusterId = 0;

            foreach (var clusterGroup in clusterGroups)
            {
                if (false && (clusterGroup.Count() > maxClusterSize))
                {
                    // Get the data points for this cluster
                    var clusterEmbeddings = clusterGroup.Select(index => embeddings[index]).ToList();

                    // Recursively call ClusterConditions for this cluster
                    var subClusterAssignments = ClusterConditions(clusterEmbeddings, maxClusters, maxClusterSize);

                    // Adjust the cluster IDs and add to the final assignments
                    foreach (var subAssignment in subClusterAssignments)
                    {
                        adjustedAssignments.Add(currentClusterId + subAssignment);
                    }

                    currentClusterId += subClusterAssignments.Max() + 1; // Update the currentClusterId
                }
                else
                {
                    // Assign the current cluster ID to all elements in this group
                    foreach (var index in clusterGroup)
                    {
                        adjustedAssignments.Add(currentClusterId);
                    }

                    currentClusterId++;
                }
            }

            return adjustedAssignments;
        }

        private int FindElbowPoint(List<double> wcssList)
        {
            int optimalK = 1;
            double maxDiff = double.MinValue;

            for (int i = 1; i < wcssList.Count - 1; i++)
            {
                double diff = wcssList[i - 1] - wcssList[i];
                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    optimalK = i + 1;
                }
            }

            return optimalK;
        }

        private double CalculateWCSS(double[][] data, double[][] centroids, int[] labels)
        {
            double wcss = 0.0;
            for (int i = 0; i < data.Length; i++)
            {
                double distance = new SquareEuclidean().Distance(data[i], centroids[labels[i]]);
                wcss += distance * distance;
            }
            return wcss;
        }
    }
}

