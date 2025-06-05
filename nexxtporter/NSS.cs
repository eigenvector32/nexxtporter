namespace nexxtporter
{
    class NSSConfig
    {
        public string? SourceFile { get; set; }
        public IList<ExportCHRConfig>? ExportCHR { get; set; }
        public IList<ExportPaletteConfig>? ExportPalette { get; set; }
        public IList<ExportNametableConfig>? ExportNametable { get; set; }
        public IList<ExportNametableAttributesConfig>? ExportNametableAttributes { get; set; }
        public IList<ExportBitmapConfig>? ExportBitmap { get; set; }
    }

    static class NSS
    {
        public static async Task ProcessNSS(Logger log, NexxtporterConfig config, NSSConfig nss, List<RGBLookup> rgbLookupTables)
        {
            if (string.IsNullOrWhiteSpace(nss.SourceFile))
            {
                log.LogError("NSSConfig missing or empty SourceFile attribute", null);
                return;
            }
            log.Log(string.Format("Opening NSS file: {0}", nss.SourceFile));

            StreamReader? reader = null;
            try
            {
                reader = new StreamReader(nss.SourceFile);

                Dictionary<string, string> tokens = new Dictionary<string, string>();
                log.Log(string.Format("Parsing NSS file: {0}", nss.SourceFile));
                while (!reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string[] tokenSplit = line.Split('=');
                        if (tokenSplit == null || tokenSplit.Length != 2)
                        {
                            continue;
                        }
                        tokens.Add(tokenSplit[0], tokenSplit[1]);
                    }
                }
                log.Log(string.Format("Finished parsing NSS file: {0} found {1} tokens", nss.SourceFile, tokens.Keys.Count.ToString()));

                if (nss.ExportCHR != null)
                {
                    foreach (ExportCHRConfig exportCHR in nss.ExportCHR)
                    {
                        await ExportCHR.ProcessExportCHR(log, config, nss, exportCHR, tokens);
                    }
                }
                if (nss.ExportPalette != null)
                {
                    foreach (ExportPaletteConfig exportPalette in nss.ExportPalette)
                    {
                        await ExportPalette.ProcessExportPalette(log, config, nss, exportPalette, tokens);
                    }
                }
                if(nss.ExportNametable != null)
                {
                    foreach(ExportNametableConfig exportNametable in nss.ExportNametable)
                    {
                        await ExportNametable.ProcessExportNametable(log, config, nss, exportNametable, tokens);
                    }
                }
                if(nss.ExportNametableAttributes != null)
                {
                    foreach(ExportNametableAttributesConfig exportNametableAttributes in nss.ExportNametableAttributes)
                    {
                        await ExportNametableAttributes.ProcessExportNametableAttributes(log, config, nss, exportNametableAttributes, tokens);
                    }
                }
                if(nss.ExportBitmap != null)
                {
                    foreach(ExportBitmapConfig exportBitmap in nss.ExportBitmap)
                    {
                        await ExportBitmap.ProcessExportBitmap(log, config, nss, exportBitmap, tokens, rgbLookupTables);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(string.Format("Error reading NSS file: {0}", nss.SourceFile), e);
                return;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }
        }
    }
}
