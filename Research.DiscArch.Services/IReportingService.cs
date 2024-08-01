namespace Research.DiscArch.Services
{
    public enum Verbosity
    { 
        Results,
        Details
    }

    public interface IReportingService
	{
		void Writeline(string text = "", Verbosity verbosity = Verbosity.Details, bool echo = true);
        void RecordStat(string key, double value);
        void WriteStats(bool echo = true);
        Verbosity Verbosity { get; set; } 
    }
}

