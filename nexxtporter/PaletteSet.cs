namespace nexxtporter
{
    record PaletteSet
    {
        public static PaletteSet? ParsePaletteSet(byte[] rawData, int rawDataStart)
        {
            if (rawDataStart + 16 > rawData.Length)
            {
                return null;
            }
            Palette[] palettes = new Palette[4];
            for (int i = 0; i < 4; i++)
            {
                Palette? palette = Palette.ParsePalette(rawData, rawDataStart + 4 * i);
                if(palette == null)
                {
                    return null;
                }
                palettes[i] = palette;
            }
            return new PaletteSet(rawData, rawDataStart, palettes);
        }

        public PaletteSet(byte[] rawData, int rawDataStart, Palette[] palettes)
        {
            if (palettes.Length != 4)
            {
                throw new ArgumentException("palettes must contain exactly 4 palettes", "palettes");
            }

            RawData = rawData;
            RawDataStart = rawDataStart;
            Palettes = palettes;
        }

        public byte[] RawData { get; init; }
        public int RawDataStart { get; init; }

        public Palette[] Palettes { get; init; }
    }
}
