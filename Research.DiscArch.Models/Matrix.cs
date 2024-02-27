namespace Research.DiscArch.Models
{
    public class Matrix
    {
        private readonly Dictionary<string, Dictionary<string, int>> rows = new();

        public Dictionary<string, string> RowGroups { get; set; } = new();

        public void SetElement(string rowKey, string columnKey, int value)
        {
            if (!rows.ContainsKey(rowKey))
                rows[rowKey] = new Dictionary<string, int>();

            rows[rowKey][columnKey] = value;
        }

        public int GetElement(string rowKey, string columnKey)
        {
            if (rows.ContainsKey(rowKey) && rows[rowKey].ContainsKey(columnKey))
            {
                return rows[rowKey][columnKey];
            }
            else
            {
                throw new Exception("Row or Column key not found");
            }
        }

        public IEnumerable<KeyValuePair<string, Dictionary<string, int>>> GetRows()
        {
            foreach (var row in rows)
            {
                yield return row;
            }
        }

        public Dictionary<string, Dictionary<string, int>> GetRowsByGroup(string group)
        {
            Dictionary<string, Dictionary<string, int>> groupRows = new();
            foreach (var rowKey in RowGroups.Where(rg => rg.Value == group).Select(rg => rg.Key))
            {
                groupRows.Add(rowKey, rows[rowKey]);
            }

            return groupRows;
        }
    }
}

