using DataRequestPipeline.Core;
using DataRequestPipeline.DataContracts;
using Microsoft.Data.SqlClient;
using System.Composition;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TestPlugins
{
    // POCO for deserializing JSON config.
    public class TestScriptConfig
    {
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }

        [JsonPropertyName("sqlFiles")]
        public List<string> SqlFiles { get; set; }

        [JsonPropertyName("resultTable")]
        public string ResultTable { get; set; }
    }


    [Export(typeof(DataRequestPipeline.DataContracts.ITestPlugin))]
    public class GenericTestRunnerPlugin : ITestPlugin
    {
        public async Task ExecuteAsync(TestContext context)
        {
            Logger.Log("GenericTestRunnerPlugin: Executing Tests...");
            // Example action: create a temporary table.
            //context.AddedTableNames.Add("TempTable1");

            var results = await ExecuteScriptsAndFetchResultsAsync("testScriptsConfig.json", context);
            context.TestsResults = results;
            context.TestsPassed = true;
            Logger.Log("GenericTestRunnerPlugin: Tests complete.");
        }

        /// <summary>
        /// Reads the configuration file, executes the listed SQL scripts in order,
        /// and then queries the specified result table to return all rows.
        /// </summary>
        /// <param name="configFilePath">Path to the JSON configuration file.</param>
        /// <returns>A list of rows where each row is represented as a dictionary (column name -> value).</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteScriptsAndFetchResultsAsync(string configFilePath, TestContext context)
        {
            Logger.Log("GenericTestRunnerPlugin : Config File : " + configFilePath);

            // Read and deserialize configuration.
            if (!File.Exists(configFilePath))
            {
                Logger.Log(configFilePath + " not found");
                throw new FileNotFoundException("Configuration file not found.", configFilePath);
            }

            string json = await File.ReadAllTextAsync(configFilePath);
            TestScriptConfig config = JsonSerializer.Deserialize<TestScriptConfig>(json);
            if (config == null)
            {
                throw new Exception("Failed to deserialize the configuration file.");
            }

            // Execute each SQL script in order.
            foreach (string sqlFile in config.SqlFiles)
            {
                if (!File.Exists(sqlFile))
                {
                    throw new FileNotFoundException("SQL file not found.", sqlFile);
                }
                Logger.Log("Executing SQL file: " + sqlFile);

                string sqlCommandText = await File.ReadAllTextAsync(sqlFile);

                using (var connection = new SqlConnection(config.ConnectionString)) // Simplified 'new' expression
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(sqlCommandText, connection)) // Simplified 'new' expression
                    {
                        command.CommandTimeout = 0; // Optional: disable timeout if scripts may run long.
                        Console.WriteLine($"Executing SQL file: {sqlFile}");
                        await command.ExecuteNonQueryAsync();
                        Logger.Log("SQL file executed successfully");
                    }
                }
            }

            // Now query the result table.
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
            using (var connection = new SqlConnection(config.ConnectionString)) // Simplified 'new' expression
            {
                await connection.OpenAsync();
                string query = $"SELECT * FROM {config.ResultTable}";
                Logger.Log("Getting Results : " + query);
                using (var command = new SqlCommand(query, connection)) // Simplified 'new' expression
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                                Logger.Log($"{reader.GetName(i)} = {reader.GetValue(i)}");
                            }
                            results.Add(row);
                        }
                    }
                }
            }

            return results;
        }
    }
}
