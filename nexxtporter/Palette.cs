namespace nexxtporter
{
    record Palette
    {
        public static Palette? ParsePalette(byte[] rawData, int rawDataStart)
        {
            if (rawDataStart + 4 > rawData.Length)
            {
                return null;
            }
            byte[] parsedData = new byte[4];
            for(int i=0;i<4;i++)
            {
                parsedData[i] = rawData[i + rawDataStart];
            }
            return new Palette(rawData, rawDataStart, parsedData);
        }

        public Palette(byte[] rawData, int rawDataStart, byte[] parsedData)
        {
            if (parsedData.Length != 4)
            {
                throw new ArgumentException("parsedData must contain exactly 4 bytes", "parsedData");
            }

            RawData = rawData;
            RawDataStart = rawDataStart;
            ParsedData = parsedData;
        }

        public byte[] RawData { get; init; }
        public int RawDataStart { get; init; }

        public byte[] ParsedData { get; init; }
    }
}
