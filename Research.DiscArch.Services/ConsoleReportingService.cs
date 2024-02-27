using System;
namespace Research.DiscArch.Services
{
    public class ConsoleReportingService : IReportingService
    {
        public void Writeline(string text = "")
        {
            Console.WriteLine(text);
        }
    }
}

