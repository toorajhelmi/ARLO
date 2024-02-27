using System.Globalization;
using CsvHelper;
using Research.DiscArch.Models;

namespace Research.DiscArch.TestData;

public enum SystemNames
{
    Messaging,
    Kaggle,
    Contradictory,
    AppFlowy,
    MLonBL,
    SmallMessagingSystem,
    OfficerDispatcher
}
public class ResourceManager
{
    public static string LoadRequirments(SystemNames systemName)
    {
        return systemName switch
        {
            SystemNames.Messaging => LoadFromTextFile("MessagingReqs.txt"),
            SystemNames.Kaggle => LoadFromTextFile("KaggleReq.txt"),
            SystemNames.Contradictory => LoadFromTextFile("ContradictoryReqs.txt"),
            SystemNames.AppFlowy => LoadFromTextFile("AppFlowy.txt"),
            SystemNames.MLonBL => LoadFromTextFile("MLonBL.txt"),
            SystemNames.OfficerDispatcher => LoadFromTextFile("OfficerDispatcher.txt"),
            SystemNames.SmallMessagingSystem => LoadFromTextFile("SmallMessagingSystem.txt"),
            _ => throw new Exception("System not found"),
        };
    }

    public static Matrix LoadArchPattenMatrix(string filePath = "quality_archipattern_matrix.csv")
    {
        Matrix qualityArchPatternmatrix = new Matrix();
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            try
            {
                //Read CSV Header Row
                csv.Read();
                var qualities = new List<string>();

                for (int i = 2; i < 100; i++)
                {
                    var quality = "";
                    if (!csv.TryGetField(i, out quality))
                        break;
                    else
                        qualities.Add(quality);
                }

                //Read CSV content
                while (csv.Read())
                {
                    string group = csv.GetField<string>(0);
                    string pattern = csv.GetField<string>(1);
                    qualityArchPatternmatrix.RowGroups[pattern] = group;

                    foreach (var quality in qualities)
                    {
                        var value = csv.GetField<int>(qualities.IndexOf(quality) + 2);
                        qualityArchPatternmatrix.SetElement(pattern, quality, value);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return qualityArchPatternmatrix;
    }

    private static string LoadFromTextFile(string filePath)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            return content;
        }
        else
        {
            throw new Exception("File not found");
        }
    }
}
