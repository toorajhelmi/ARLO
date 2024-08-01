using Research.DiscArch.Models;

namespace Research.DiscArch.TestData;

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
            SystemNames.Bamboo => LoadFromTextFile("Bamboo.txt"),
            SystemNames.Mule => LoadFromTextFile("Mule.txt"),
            SystemNames.Aptana => LoadFromTextFile("aptana.txt"),
            SystemNames.SpringXd => LoadFromTextFile("springxd.txt"),
            _ => throw new Exception("System not found"),
        };
    }

    public static Matrix LoadArchPattenMatrix(string filePath = "quality_archipattern_matrix.csv", bool commaSeperated = false)
    {
        var QAs = new Dictionary<string, string> {
            { "PE", "Performance Efficiency" },
            { "CO", "Compatibility" },
            { "US", "Usability" },
            { "RE", "Reliability" },
            { "SE", "Security" },
            { "MA", "Maintainability" },
            { "PO", "Portability" },
            { "CE", "Cost Efficiency" }
        };

        Matrix qualityArchPatternmatrix = new Matrix();

        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            var lines = File.ReadAllLines(path);

            var qualitiesAcronyms = lines[0].Split(commaSeperated ? ',' : '\t').Select(v => v.Trim()).Where(v => v != "").ToList();
            var qualities = qualitiesAcronyms.Select(qaa => QAs[qaa]).ToList();

            foreach (var line in lines.Skip(1))
            {
                var values = line.Split(commaSeperated ? ',' : '\t').Select(v => v.Trim()).Where(v => v != "").ToList();
                string group = values[0];
                string pattern = values[1];
                qualityArchPatternmatrix.RowGroups[pattern] = group;

                for (int i = 2; i < values.Count; i++)
                {
                    var quality = qualities[i - 2];
                    qualityArchPatternmatrix.SetElement(pattern, quality, int.Parse(values[i]));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    //using (var reader = new StreamReader(path))
    //using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    //{
    //    try
    //    {
    //        //Read CSV Header Row
    //        csv.Read();
    //        var qualities = new List<string>();

    //        for (int i = 2; i < 100; i++)
    //        {
    //            var quality = "";
    //            if (!csv.TryGetField(i, out quality))
    //                break;
    //            else
    //                qualities.Add(quality);
    //        }

    //        //Read CSV content
    //        while (csv.Read())
    //        {
    //            string group = csv.GetField<string>(0);
    //            string pattern = csv.GetField<string>(1);
    //            qualityArchPatternmatrix.RowGroups[pattern] = group;

    //            foreach (var quality in qualities)
    //            {
    //                var value = csv.GetField<int>(qualities.IndexOf(quality) + 2);
    //                qualityArchPatternmatrix.SetElement(pattern, quality, value);
    //            }
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(e);
    //    }
    //}

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
