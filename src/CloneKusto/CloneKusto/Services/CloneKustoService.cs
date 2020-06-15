using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloneKusto.Models;
using Kusto.Data.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CloneKusto.Services
{
    public class CloneKustoService : ICloneKustoService
    {
        private readonly IKustoClientProvider _clientProvider;
        private readonly ILogger<CloneKustoService> _log;

        public CloneKustoService(
            IKustoClientProvider clientProvider,
            ILogger<CloneKustoService> log)
        {
            _clientProvider = clientProvider;
            _log = log;
        }

        public async Task CloneAsync(CloneOptions options, CancellationToken cancellationToken)
        {
            var outputFolder = options.OutputDirectory ?? Directory.GetCurrentDirectory();

            _log.LogInformation(
                $"Cloning schema from the following databases: {string.Join(',', options.DatabaseNames)}. " +
                $"Cluster URI: {options.ClusterUri}, Output Path: {outputFolder}");

            foreach (var databaseName in options.DatabaseNames)
            {
                var client = _clientProvider.GetClient(databaseName);
                var database = await GetDatabaseSchemaAsync(databaseName, client);
                var ingestionMappings = await GetIngestionMappingsAsync(databaseName, client);

                var databaseOutputFolder = Path.Combine(outputFolder, "db", databaseName);
                await WriteFunctionsAsync(databaseOutputFolder, database.Functions.Values, cancellationToken);
                await WriteTablesAsync(databaseOutputFolder, database.Tables.Values, ingestionMappings, cancellationToken);

                _log.LogDebug($"Finished cloning schema for database '{databaseName}");
            }

            _log.LogInformation($"Successfully cloned schema(s) for the following database(s): {string.Join(',', options.DatabaseNames)}");
        }

        private async Task<DatabaseSchema> GetDatabaseSchemaAsync(string databaseName, ICslAdminProvider client)
        {
            _log.LogDebug($"Getting schema for database '{databaseName}'");

            var showDatabaseSchemaCsl = $".show database ['{databaseName}'] schema as json";
            ClusterSchema clusterSchema;
            using (var reader = await client.ExecuteControlCommandAsync(databaseName, showDatabaseSchemaCsl))
            {
                reader.Read();
                clusterSchema = JsonConvert.DeserializeObject<ClusterSchema>(reader[0].ToString());
            }

            _log.LogDebug($"Successfully retrieved schema for database '{databaseName}'");

            return clusterSchema.Databases[databaseName];
        }

        private async Task<IReadOnlyDictionary<string, IngestionMapping[]>> GetIngestionMappingsAsync(
            string databaseName,
            ICslAdminProvider client)
        {
            _log.LogDebug($"Getting ingestion mappings for database '{databaseName}'");

            var showIngestionMappingsCsl = $".show databases (['{databaseName}']) ingestion mappings with (onlyLatestPerTable=False)";
            var ingestionMappings = new List<IngestionMapping>();
            using (var reader = await client.ExecuteControlCommandAsync(databaseName, showIngestionMappingsCsl))
            {
                while (reader.Read())
                {
                    var mapping = new IngestionMapping(
                        reader.GetString(5),
                        reader.GetString(0),
                        reader.GetString(1),
                        JsonConvert.DeserializeObject<IngestionMappingColumnMapping[]>(reader.GetString(2)));

                    ingestionMappings.Add(mapping);
                }
            }

            _log.LogDebug($"Successfully retrieved ingestion mappings for database '{databaseName}'");

            return ingestionMappings.GroupBy(m => m.TableName).ToDictionary(grp => grp.Key, grp => grp.ToArray());
        }

        private Task WriteFunctionsAsync(
            string outputFolder,
            IEnumerable<FunctionSchema> functions,
            CancellationToken cancellationToken)
        {
            return WriteSchemaToFileAsync(
                Path.Combine(outputFolder, "Functions"),
                functions.ToArray(),
                f => f.Name,
                f => f.Folder,
                f => CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(f, false),
                cancellationToken);
        }

        private Task WriteTablesAsync(
            string outputFolder,
            IEnumerable<TableSchema> tables,
            IReadOnlyDictionary<string, IngestionMapping[]> ingestionMappingLookup,
            CancellationToken cancellationToken)
        {
            return WriteSchemaToFileAsync(
                Path.Combine(outputFolder, "Tables"),
                tables.ToArray(),
                t => t.Name,
                t => t.Folder,
                t =>
                {
                    // Add new lines so we generate a file that will diff nicely in source control
                    var createOrMergeTableCommand = CslCommandGenerator
                        .GenerateTableCreateMergeCommand(t)
                        .Replace("(", "(\n  ")
                        .Replace(", ", ",\n  ")
                        .Replace(")", "\n)");

                    var builder = new StringBuilder();
                    builder.AppendLine(createOrMergeTableCommand);

                    if (ingestionMappingLookup.TryGetValue(t.Name, out var ingestionMappings))
                    {
                        foreach (var ingestionMapping in ingestionMappings.OrderBy(m => m.Kind).ThenBy(m => m.Name))
                        {
                            builder.AppendLine();
                            builder.AppendLine();
                            builder.AppendLine($".create-or-alter table {t.Name} ingestion {ingestionMapping.Kind.ToLower()} mapping \"{ingestionMapping.Name}\"");
                            builder.AppendLine("'['");
                            for (var i = 0; i < ingestionMapping.ColumnMappings.Count; i++)
                            {
                                var columnMapping = ingestionMapping.ColumnMappings[i];
                                builder.Append($"'  {JsonConvert.SerializeObject(columnMapping, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })}");
                                builder.Append(i == ingestionMapping.ColumnMappings.Count - 1 ? "" : ",");
                                builder.AppendLine("'");
                            }
                            builder.AppendLine("']'");
                        }
                    }

                    return builder.ToString();
                },
                cancellationToken);
        }

        private async Task WriteSchemaToFileAsync<TSchema>(
            string rootFolder,
            IReadOnlyList<TSchema> schemaObjects,
            Func<TSchema, string> getObjectNameFunc,
            Func<TSchema, string> getObjectFolderFunc,
            Func<TSchema, string> getObjectCslFunc,
            CancellationToken cancellationToken)
        {
            _log.LogDebug($"Writing {schemaObjects.Count} objects to {rootFolder}");

            foreach (var schemaObject in schemaObjects)
            {
                var objectName = getObjectNameFunc(schemaObject);
                var objectFolder = getObjectFolderFunc(schemaObject) ?? "";
                var objectCsl = getObjectCslFunc(schemaObject);

                var fileName = $"{objectName}.csl";
                // First remove any other files with this name. In the case where an object has moved to a new folder,
                // this will handle cleaning up the old file
                var existingFiles = Directory.Exists(rootFolder)
                    ? Directory.GetFiles(rootFolder, fileName, SearchOption.AllDirectories)
                    : new string[0];
                if (existingFiles.Length > 0)
                {
                    foreach (string file in existingFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // It's not the end of the world if this call fails
                        }
                    }
                }

                // Now write the new file to the correct location.
                var destinationFolder = Path.Combine(rootFolder, objectFolder);
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                var destinationFilePath = Path.Combine(destinationFolder, fileName);
                await File.WriteAllTextAsync(destinationFilePath, objectCsl, cancellationToken);
            }
        }
    }
}