using DataRequestPipeline.Core;
using DataRequestPipeline.DataContracts;
using System.ComponentModel.Composition;


namespace SetupPlugins
{
    [Export(typeof(ISetupPlugin))]
    public class SetupPluginA : ISetupPlugin
    {
        public SetupPluginA()
        {
            var contractAssembly = typeof(DataRequestPipeline.DataContracts.ISetupPlugin).Assembly;
            Console.WriteLine("Plugin sees contracts assembly at: " + contractAssembly.Location);
            Console.WriteLine("Full name: " + contractAssembly.FullName);

            var pluginContractAssembly = typeof(DataRequestPipeline.DataContracts.ISetupPlugin).Assembly;
            Console.WriteLine("Plugin contract assembly: " + pluginContractAssembly.Location);

        }

        public async Task ExecuteAsync(SetupContext context)
        {
            Logger.Log("SetupPluginA: Executing setup...");
            // Example action: create a temporary table.
            context.AddedTableNames.Add("TempTable1");
            await Task.Delay(500); // Simulate async work.
            Logger.Log("SetupPluginA: Setup complete.");
        }

        public async Task RollbackAsync(SetupContext context)
        {
            Logger.Log("SetupPluginA: Rolling back setup...");
            // Example rollback: remove the temporary table.
            if (context.AddedTableNames.Contains("TempTable1"))
            {
                context.AddedTableNames.Remove("TempTable1");
            }
            await Task.Delay(200); // Simulate async work.
            Logger.Log("SetupPluginA: Rollback complete.");
        }
    }
}
