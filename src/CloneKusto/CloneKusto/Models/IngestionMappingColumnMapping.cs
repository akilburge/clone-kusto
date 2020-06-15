using System.Runtime.Serialization;

namespace CloneKusto.Models
{
    [DataContract]
    public class IngestionMappingColumnMapping
    {
        // Json
        [DataMember(Name = "column")]
        public string ColumnName { get; set; }
        [DataMember(Name = "path")]
        public string JsonPath { get; set; }
        [DataMember(Name = "datatype")]
        public string ColumnType { get; set; }
        [DataMember(Name = "transform")]
        public string TransformationMethod { get; set; }

        // Csv
        [DataMember(Name = "Name")]
        public string Name { get; set; }
        [DataMember(Name = "DataType")]
        public string CslDataType { get; set; }
        [DataMember(Name = "CsvDataType")]
        public string CsvColumnDataType { get; set; }
        [DataMember(Name = "Ordinal")]
        public int? Ordinal { get; set; }
        [DataMember(Name = "ConstValue")]
        public string ConstValue { get; set; }

        // Avro
        [DataMember(Name = "field")]
        public string FieldName { get; set; }
    }
}