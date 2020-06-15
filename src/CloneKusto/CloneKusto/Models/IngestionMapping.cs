using System.Collections.Generic;

namespace CloneKusto.Models
{
    public class IngestionMapping
    {
        public string TableName { get; }
        public string Name { get; }
        public string Kind { get; }
        public IReadOnlyList<IngestionMappingColumnMapping> ColumnMappings { get; }

        public IngestionMapping(
            string tableName,
            string name,
            string kind,
            IReadOnlyList<IngestionMappingColumnMapping> columnMappings)
        {
            TableName = tableName;
            Name = name;
            Kind = kind;
            ColumnMappings = columnMappings;
        }
    }
}
