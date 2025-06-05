namespace nexxtporter
{
    record Pattern
    {
        public static Pattern? ParsePattern(byte[] rawData, int rawDataStart)
        {
            if (rawDataStart + 16 > rawData.Length)
            {
                return null;
            }
            byte[] parsedData = new byte[64];
            int index = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    // https://www.nesdev.org/wiki/PPU_pattern_tables
                    // Result will be a number 0-4
                    // Low bit is from the corresponding bit in source bytes 0-7
                    // High bit is from the corresponding bit in source bytes 8-F
                    bool low = rawData[y + rawDataStart].TestBit(7 - x);
                    bool high = rawData[y + rawDataStart + 8].TestBit(7 - x);

                    parsedData[index] = 0;
                    if(low)
                    {
                        parsedData[index] += 1;
                    }
                    if(high)
                    {
                        parsedData[index] += 2;
                    }
                    
                    index++;
                }
            }
            return new Pattern(rawData, rawDataStart, parsedData);
        }

        public Pattern(byte[] rawData, int rawDataStart, byte[] parsedData)
        {
            if (parsedData.Length != 64)
            {
                throw new ArgumentException("parsedData must contain exactly 64 bytes", "parsedData");
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
