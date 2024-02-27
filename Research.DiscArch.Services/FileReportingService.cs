namespace Research.DiscArch.Services
{
    public class FileReportingService : IReportingService
	{
        private string fileName;

		public FileReportingService()
		{
            if (!Directory.Exists("Reports"))
                Directory.CreateDirectory("Reports");

            fileName = $"./Reports/{DateTime.Now.ToLongDateString()}-{DateTime.Now.ToLongTimeString()}";
            File.WriteAllText(fileName, "");

            Console.WriteLine($"File Reporing Service Add. Path: {fileName}");
		}

        public void Writeline(string text = "")
        {
            File.AppendAllText(fileName, text);
            File.AppendAllText(fileName, "\r\n");
        }
    }
}

