using Google.OrTools.LinearSolver;
using Research.DiscArch.Models;

namespace Research.DiscArch.Designer
{
    public enum OptimizerMode
    {
        ILP,
        Greedy
    }

    public class Optimizer
    {
        public (List<Decision> decision, Dictionary<string, int> satisfactionScores) Optimize(OptimizerMode optimizerMode, List<string> desiredQualities, Matrix matrix, Dictionary<string, int> columnWeights)
        {
            return optimizerMode switch
            {
                OptimizerMode.Greedy => Greedy(desiredQualities, matrix, columnWeights),
                OptimizerMode.ILP => Ilp(desiredQualities, matrix, columnWeights),
                _ => throw new Exception("Not supported"),
            };
        }

        private (List<Decision> decision, Dictionary<string, int> satisfactionScores) Greedy(List<string> desiredQualities, Matrix matrix, Dictionary<string, int> columnWeights)
        {
            var decisions = new List<Decision>();

            foreach (var group in matrix.RowGroups.GroupBy(kv => kv.Value).Select(g => g.Key))
            {
                var decision = new Decision { ArchPatternName = group, Score = int.MinValue };
                decisions.Add(decision);

                foreach (var row in matrix.GetRowsByGroup(group))
                {
                    var satisfiedQualities = new List<(string, int)>();
                    var unsatisfiedQualities = new List<(string, int)>();
                    int rowValue = 0;

                    foreach (var column in row.Value)
                    {
                        if (desiredQualities.Contains(column.Key))
                        {
                            rowValue += column.Value * columnWeights[column.Key];
                            if (column.Value > 0)
                                satisfiedQualities.Add((column.Key, column.Value));
                            else if (column.Value < 0)
                                unsatisfiedQualities.Add((column.Key, column.Value));
                        }
                    }

                    if (rowValue > decision.Score)
                    {
                        decision.Score = rowValue;
                        decision.SelectedPattern = row.Key;
                        decision.SatisfiedQualties = satisfiedQualities;
                        decision.UnsatisfiedQualties = unsatisfiedQualities;
                    }
                }
            }

            return (decisions, CalculateSatifcationScores(matrix, columnWeights, decisions));
        }


        private (List<Decision> decision, Dictionary<string, int> satisfactionScores) Ilp(List<string> desiredQualities, Matrix matrix, Dictionary<string, int> columnWeights)
        {
            Solver solver = Solver.CreateSolver("SCIP");

            if (solver == null)
            {
                Console.WriteLine("Could not create solver.");
                return default;
            }

            Dictionary<string, Variable> variables = new();
            foreach (var row in matrix.GetRows())
            {
                variables[row.Key] = solver.MakeIntVar(0, 1, row.Key);
            }

            foreach (var group in matrix.RowGroups.Values.Distinct())
            {
                var groupRows = matrix.GetRowsByGroup(group);
                Constraint constraint = solver.MakeConstraint(1, 1, $"OnlyOneRowInGroup_{group}");
                foreach (var row in groupRows)
                {
                    constraint.SetCoefficient(variables[row.Key], 1);
                }
            }

            Objective objective = solver.Objective();
            foreach (var row in matrix.GetRows())
            {
                int rowScore = row.Value.Where(c => desiredQualities.Contains(c.Key))
                                        .Sum(c => c.Value * columnWeights.GetValueOrDefault(c.Key, 0));
                objective.SetCoefficient(variables[row.Key], rowScore);
            }
            objective.SetMaximization();

            Solver.ResultStatus resultStatus = solver.Solve();

            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                Console.WriteLine("The problem does not have an optimal solution.");
                return default;
            }

            List<Decision> decisions = new List<Decision>();
            foreach (var group in matrix.RowGroups.Values.Distinct())
            {
                var groupRows = matrix.GetRowsByGroup(group);
                foreach (var row in groupRows)
                {
                    if (variables[row.Key].SolutionValue() == 1)
                    {
                        var satisfiedQualities = new List<(string, int)>();
                        var unsatisfiedQualities = new List<(string, int)>();
                        int rowValue = 0;

                        foreach (var column in row.Value)
                        {
                            if (desiredQualities.Contains(column.Key))
                            {
                                rowValue += column.Value * columnWeights[column.Key];
                                if (column.Value > 0)
                                    satisfiedQualities.Add((column.Key, column.Value));
                                else if (column.Value < 0)
                                    unsatisfiedQualities.Add((column.Key, column.Value));
                            }
                        }

                        var decision = new Decision
                        {
                            ArchPatternName = group,
                            SelectedPattern = row.Key,
                            Score = row.Value.Where(c => desiredQualities.Contains(c.Key))
                                             .Sum(c => c.Value * columnWeights.GetValueOrDefault(c.Key, 0)),
                            SatisfiedQualties = satisfiedQualities,
                            UnsatisfiedQualties = unsatisfiedQualities
                        };
                        decisions.Add(decision);
                        break;
                    }
                }
            }

            return (decisions, CalculateSatifcationScores(matrix, columnWeights, decisions));
        }

        private static Dictionary<string, int> CalculateSatifcationScores(Matrix matrix, Dictionary<string, int> columnWeights, List<Decision> decisions)
        {
            Dictionary<string, int> satisfactionScores = new();

            foreach (var decision in decisions)
            {
                var selectedRow = matrix.GetRowsByGroup(decision.ArchPatternName)
                                        .FirstOrDefault(r => r.Key == decision.SelectedPattern);
                if (selectedRow.Value != null)
                {
                    foreach (var column in selectedRow.Value)
                    {
                        if (!satisfactionScores.ContainsKey(column.Key))
                        {
                            satisfactionScores[column.Key] = 0;
                        }

                        satisfactionScores[column.Key] += column.Value * columnWeights.GetValueOrDefault(column.Key, 0);
                    }
                }
            }

            return satisfactionScores;
        }
    }
}

