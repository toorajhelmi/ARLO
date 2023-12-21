using System.Collections;
using System.Formats.Asn1;
using System.Globalization;
using System.Xml.Serialization;
using CsvHelper;
using Research.DiscArch.Models;

namespace Research.DiscArch.TestData;

public enum SystemNames
{
    Messaging,
    Kaggle,
    Contradictory
}
public class ResourceManager
{
    public static string LoadRequirments(SystemNames systemName)
    {
        switch (systemName)
        {
            case SystemNames.Messaging:
                return LoadFromTextFile("MessagingReqs.txt");
            case SystemNames.Kaggle:
                return LoadFromTextFile("KaggleReq.txt");
            case SystemNames.Contradictory:
                return LoadFromTextFile("ContradictoryReqs.txt");
            default:
                throw new Exception("System not found");
        }
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
