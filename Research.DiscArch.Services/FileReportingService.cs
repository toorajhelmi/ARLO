using Research.DiscArch.Models;

namespace Research.DiscArch.Services
{
    public class FileReportingService : IReportingService
	{
        private string fileName;
        private Dictionary<string, double> stats = new();

        public Verbosity Verbosity { get; set; }

        public FileReportingService(ExperimentSettings experimentSettings, Verbosity verbosity = Verbosity.Details)
		{
            this.Verbosity = verbosity;

            if (!Directory.Exists("Reports"))
                Directory.CreateDirectory("Reports");

            fileName = $"./Reports/{experimentSettings.ExpermentName}-{experimentSettings.ReportsTag}-{DateTime.Now.ToLongDateString()}-{DateTime.Now.ToLongTimeString()}";
            File.WriteAllText(fileName, "");

            Console.WriteLine($"File Reporing Service Add. Path: {fileName}");
		}

        public void Writeline(string text = "", Verbosity verbosity = Verbosity.Details, bool echo = true)
        {
            if (Verbosity == Verbosity.Details || verbosity == Verbosity.Results)
            {
                File.AppendAllText(fileName, text);
                File.AppendAllText(fileName, "\r\n");
                if (echo)
                {
                    Console.WriteLine(text);
                }
            }
        }

        public void RecordStat(string key, double value)
        {
            stats[key] = value;
        }

        public void WriteStats(bool echo = true)
        {
            File.AppendAllText(fileName, "\r\nStatistics\r\n");

            if (echo)
            {
                Console.WriteLine("\r\nStatistics\r\n");
            }

            foreach (KeyValuePair<string, double> stat in stats)
            { 
                File.AppendAllText(fileName, $"{stat.Key}: {stat.Value}\r\n");

                if (echo)
                {
                    Console.WriteLine($"{stat.Key}: {stat.Value}\r\n");
                }
            }
        }
    }
}

