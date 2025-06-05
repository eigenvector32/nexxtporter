namespace nexxtporter
{
    class ExportCHRConfig
    {
        public string? TargetFile { get; set; }
        public string? Start { get; set; }
        public string? Size { get; set; }
    }

    static class ExportCHR
    {
        public static async Task ProcessExportCHR(Logger log, NexxtporterConfig config, NSSConfig nssConfig, ExportCHRConfig chrConfig, Dictionary<string, string> tokens)
        {
            if (string.IsNullOrWhiteSpace(chrConfig.TargetFile))
            {
                log.LogError("ExportCHRConfig missing or empty TargetFile attribute", null);
                return;
            }
            if (!tokens.ContainsKey("CHRMain"))
            {
                log.LogError(string.Format("NSS file {0} does not contain CHRMain token, unable to export CHR to {1}", nssConfig.SourceFile, chrConfig.TargetFile), null);
                return;
            }

            int start = 0;
            int size = 1024;

            if (!string.IsNullOrWhiteSpace(chrConfig.Start))
            {
                if (!Utils.TryParseNumber(chrConfig.Start, out start))
                {
                    log.LogError(string.Format("NSS file {0}, CHR export to {1}, has invalid Start attribute {2}", nssConfig.SourceFile, chrConfig.TargetFile, chrConfig.Start), null);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(chrConfig.Size))
            {
                if (!Utils.TryParseNumber(chrConfig.Size, out size))
                {
                    log.LogError(string.Format("NSS file {0}, CHR export to {1}, has invalid Size attribute {2}", nssConfig.SourceFile, chrConfig.TargetFile, chrConfig.Size), null);
                    return;
                }
            }

            byte[] data = Utils.ParseRLEBinary(log, nssConfig, "CHRMain", tokens["CHRMain"]);
            if (data.Length < start + size)
            {
                log.LogError(string.Format("CHRMain is {0} bytes which is smaller than the requested start {1} and size {2}", data.Length, start, size), null);
                return;
            }

            log.Log(string.Format("Writing CHR [{0},{1}] to file {2}", start, size, chrConfig.TargetFile));


            await Utils.WriteBinaryFile(log, chrConfig.TargetFile, data, start, size);
        }
    }
}
