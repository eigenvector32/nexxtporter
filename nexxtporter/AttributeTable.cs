namespace nexxtporter
{
    record AttributeTable
    {
        public static AttributeTable? ParseAttributeTable(byte[] rawData)
        {
            if (rawData.Length != 64)
            {
                return null;
            }
            byte[] parsedData = new byte[1024];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // https://www.nesdev.org/wiki/PPU_attribute_tables
                    int attribX = x >> 2;
                    int attribY = y >> 2;
                    byte attribByte = rawData[attribY * 8 + attribX];
                    byte output = 0;
                    if (x % 4 < 2)
                    {
                        if (y % 4 < 2)
                        {
                            // Upper left
                            output = (byte)(0b00000011 & attribByte);
                        }
                        else
                        {
                            // Lower left
                            output = (byte)((0b00110000 & attribByte) >> 4);
                        }
                    }
                    else
                    {
                        if (y % 4 < 2)
                        {
                            // Upper right
                            output = (byte)((0b00001100 & attribByte) >> 2);
                        }
                        else
                        {
                            // Lower right
                            output = (byte)((0b11000000 & attribByte) >> 6);
                        }
                    }
                    parsedData[y * 32 + x] = output;
                }
            }
            return new AttributeTable(rawData, parsedData);
        }

        public AttributeTable(byte[] rawData, byte[] parsedData)
        {
            if (parsedData.Length != 1024)
            {
                throw new ArgumentException("parsedData must contain exactly 1024 bytes", "parsedData");
            }

            RawData = rawData;
            ParsedData = parsedData;
        }

        public byte[] RawData { get; init; }

        public byte[] ParsedData { get; init; }

        public byte GetForPosition(int x, int y)
        {
            if (x < 0 || x >= 32)
            {
                throw new ArgumentException(string.Format("x is out of bounds {0}", x), "x");
            }
            if (y < 0 || y >= 32)
            {
                throw new ArgumentException(string.Format("y is out of bounds {0}", y), "y");
            }
            return ParsedData[y * 32 + x];
        }
    }
}

