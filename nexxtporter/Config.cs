namespace nexxtporter
{
    class NexxtporterConfig
    {
        public LoggerConfig? LogConfig { get; set; } = new LoggerConfig();
        public IList<NSSConfig>? NSSFiles { get; set; }
        public IList<RGBLookupConfig>? RGBLookupTables { get; set; }
    }
}
