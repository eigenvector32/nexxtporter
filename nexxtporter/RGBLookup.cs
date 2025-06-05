using SkiaSharp;

namespace nexxtporter
{
    class RGBLookupConfig
    {
        public static readonly RGBLookupConfig DefaultRGBLookup = new RGBLookupConfig
            {
                ID = "Default",
                Colors = new List<string>
                {
                    "6A6D6A",
                    "001380",
                    "1E008A",
                    "39007A",
                    "550056",
                    "5A0018",
                    "4F1000",
                    "3D1C00",

                    "253200",
                    "003D00",
                    "004000",
                    "003924",
                    "002E55",
                    "000000",
                    "000000",
                    "000000",

                    "B9BCB9",
                    "1850C7",
                    "4B30E3",
                    "7322D6",
                    "951FA9",
                    "9D285C",
                    "983700",
                    "7F4C00",

                    "5E6400",
                    "227700",
                    "027E02",
                    "007645",
                    "006E8A",
                    "000000",
                    "000000",
                    "000000",

                    "FFFFFF",
                    "68A6FF",
                    "8C9CFF",
                    "B586FF",
                    "D975FD",
                    "E377B9",
                    "E58D68",
                    "D49D29",

                    "B3AF0C",
                    "7BC211",
                    "55CA47",
                    "46CB81",
                    "47C1C5",
                    "4A4D4A",
                    "000000",
                    "000000",

                    "FFFFFF",
                    "CCEAFF",
                    "DDDEFF",
                    "ECDAFF",
                    "F8D7FE",
                    "FCD6F5",
                    "FDDBCF",
                    "F9E7B5",

                    "F1F0AA",
                    "DAFAA9",
                    "C9FFBC",
                    "C3FBD7",
                    "C4F6F6",
                    "BEC1BE",
                    "000000",
                    "000000"
                }
            };

        public string? ID { get; set; }
        public IList<string>? Colors { get; set; }
    }

    record RGBLookup
    {
        public RGBLookup(RGBLookupConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.ID))
            {
                throw new ArgumentException("Missing or empty id attribute");
            }
            ID = config.ID;
            if (config.Colors == null)
            {
                throw new ArgumentException("Colors attribute is null");
            }
            if (config.Colors.Count != 64)
            {
                throw new ArgumentException("Colors attribute must contain exactly 64 entries");
            }
            SKColor[] colors = new SKColor[64];
            for (int i = 0; i < 64; i++)
            {
                colors[i] = SKColor.Parse(config.Colors[i]);
            }
            Colors = colors;
        }

        public RGBLookup(string id, SKColor[] colors)
        {
            if (colors.Length != 64)
            {
                throw new ArgumentException("colors must contain exactly 64 entries", "colors");
            }

            ID = id;
            Colors = colors;
        }

        public string ID { get; init; }
        public SKColor[] Colors { get; init; }
    }
}
