using Research.DiscArch.Models;
using Research.DiscArch.TestData;

namespace Research.DiscArch.Architect;

public class DecisionMaker
{
    private static Matrix qualityArchPatternmatrix = new Matrix();

    static DecisionMaker()
    {
        qualityArchPatternmatrix = ResourceManager.LoadArchPattenMatrix();
    }

    public DecisionMaker()
    {
    }

    public List<Decision> SelectArch(List<string> desiredQualities)
    {
        var decisions = new List<Decision>();

        foreach (var group in qualityArchPatternmatrix.RowGroups)
        {
            var decision = new Decision { ArchPatternName = group.Value };
            decisions.Add(decision);
            int maxGroupValue = int.MinValue;

            foreach (var row in qualityArchPatternmatrix.GetRowsByGroup(group.Key))
            {
                var satisfiedQualities = new List<string>();
                var unsatisfiedQualities = new List<string>();
                int rowValue = 0;

                foreach (var column in row.Value)
                {
                    if (desiredQualities.Contains(column.Key))
                    {
                        rowValue += column.Value;
                        if (column.Value > 0)
                            satisfiedQualities.Add(column.Key);
                        else if (column.Value < 0)
                            unsatisfiedQualities.Add(column.Key);
                    }
                }

                if (rowValue > maxGroupValue)
                {
                    maxGroupValue = rowValue;
                    decision.SelectedPattern = row.Key;
                    decision.SatisfiedQualties = satisfiedQualities;
                    decision.UnsatisfiedQualties = unsatisfiedQualities;
                }
            }
        }

        return decisions;
    }
}

