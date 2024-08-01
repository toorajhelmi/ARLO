//var lines = File.ReadAllLines("/Users/thelmi/Downloads/BasicArlo-Aptana");""
var lines = File.ReadAllLines("/Users/thelmi/Downloads/BasicArlo-Bamboo-b");


var stats = new Dictionary<int, int>();

for (int lineIndex = 0; lineIndex < lines.Length; lineIndex ++)
{
    int reqCount = 0;
    if (lines[lineIndex] == "--------------------------------------------------")
    {
        lineIndex++;
        if (lineIndex >= lines.Length)
            break;
        while (lines[lineIndex] != "--------------------------------------------------")
        {
            reqCount++;
            lineIndex++;
            if (lineIndex >= lines.Length)
                break;
        }
        if (!stats.ContainsKey(reqCount))
        {
            stats.Add(reqCount, 0);
        }

        stats[reqCount]++;
    }
}

foreach (var kv in stats)
{
    Console.WriteLine($"{kv.Key}: {kv.Value}");
}
Console.ReadLine();
