using System.Text.Json;

// Note the use of try / catch / finally instead of using throughout is deliberate in order to catch and log exceptions.

namespace nexxtporter
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            string sourceFile = "config.json";
            if (args != null && args.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(args[0]))
                {
                    Console.WriteLine("Config file is null or whitespace");
                    return 1;
                }
                sourceFile = args[0];
            }

            FileStream? configStream = null;
            NexxtporterConfig? config = null;
            try
            {
                configStream = File.OpenRead(sourceFile);
                config = await JsonSerializer.DeserializeAsync<NexxtporterConfig>(configStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Unable to open config file: {0}", sourceFile));
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (configStream != null)
                {
                    configStream.Dispose();
                }
            }
            if (config == null)
            {
                return 1;
            }

            Logger log = new Logger(config.LogConfig);

            List<RGBLookup> rgbLookupTables = new List<RGBLookup>();
            try
            {
                rgbLookupTables.Add(new RGBLookup(RGBLookupConfig.DefaultRGBLookup));
                if (config.RGBLookupTables != null && config.RGBLookupTables.Count > 0)
                {
                    foreach (RGBLookupConfig rgbLookupConfig in config.RGBLookupTables)
                    {
                        rgbLookupTables.Add(new RGBLookup(rgbLookupConfig));
                    }
                }
            }
            catch(Exception e)
            {
                log.LogError("Error parsing RGBLookupTables", e);
            }

            if (config.NSSFiles == null || config.NSSFiles.Count == 0)
            {
                log.LogError("Config contains no NSSFiles entries", null);
            }
            else
            {
                foreach (NSSConfig nss in config.NSSFiles)
                {
                    await NSS.ProcessNSS(log, config, nss, rgbLookupTables);
                }
            }

            await log.WriteLogFile();

            Console.WriteLine("Export Complete");

            return 0;
        }
    }
}
