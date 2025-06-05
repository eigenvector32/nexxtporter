using SkiaSharp;
using System.Drawing;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace nexxtporter
{
    static class Utils
    {
        public static bool TestBit(this byte input, int bit)
        {
            if (bit < 0 || bit >= 8)
            {
                throw new ArgumentException(string.Format("bit argument must be 0-7, received {0}", bit), "bit");
            }
            return (input & (1 << bit)) > 0;
        }

        public static RGBLookup GetByID(this IList<RGBLookup> input, string id)
        {
            for (int i = 0; i < input.Count; i++)
            {
                if (input[i].ID == id)
                {
                    return input[i];
                }
            }
            throw new ArgumentException(string.Format("ID {0} does not exist", id), "id");
        }

        public static async Task WriteByteData(Logger log, StreamWriter writer, byte[] data, int start, int size)
        {
            if (start < 0 || start + size > data.Length)
            {
                log.LogError(string.Format("Invalid start or size writing byte data [{0},{1}] from {2} byte buffer", start, size, data.Length), null);
                return;
            }
            await writer.WriteAsync(".byte ");
            for (int i = 0; i < size; i++)
            {
                await writer.WriteAsync("$");
                await writer.WriteAsync(data[start + i].ToString("X2"));
                if (i < size - 1)
                {
                    await writer.WriteAsync(",");
                }
            }
            await writer.WriteLineAsync("");
        }

        public static async Task WriteBinaryFile(Logger log, string TargetFile, byte[] data, int start, int size)
        {
            FileStream? stream = null;
            try
            {
                stream = File.Create(TargetFile, data.Length);
                await stream.WriteAsync(data, start, size);
                await stream.FlushAsync();
            }
            catch (Exception e)
            {
                log.LogError(string.Format("Error writing {0} bytes of binary to {1}", size, TargetFile), e);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public static async Task WriteSKBitmap(Logger log, string TargetFile, SKBitmap bitmap, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            FileStream? stream = null;
            try
            {
                stream = File.Create(TargetFile);

                if (!bitmap.Encode(stream, SKEncodedImageFormat.Png, quality))
                {
                    log.LogError(string.Format("SKBitmap.Encode failed for {0}", TargetFile), null);
                    return;
                }

                await stream.FlushAsync();
            }
            catch (Exception e)
            {
                log.LogError(string.Format("Error writing SKBitmap to {0}", TargetFile), e);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public static bool TryParseNumber(string? input, out int output)
        {
            output = 0;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }
            try
            {
                if (input.StartsWith('$'))
                {
                    output = int.Parse(input.Substring(1), NumberStyles.HexNumber);
                    return true;
                }
                else if (input.StartsWith('%'))
                {
                    output = int.Parse(input.Substring(1), NumberStyles.BinaryNumber);
                    return true;
                }
                else
                {
                    output = int.Parse(input);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static byte[] ParseRLEBinary(Logger log, NSSConfig nss, string token, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<byte>();
            }
            string[] segmentSplit = input.Split(new char[] { '[', ']' });

            // Count the size of the output
            int outputSize = 0;
            for (int i = 0; i < segmentSplit.Length; i++)
            {
                if (int.IsOddInteger(i))
                {
                    // RLE segment
                    int rleCount = int.Parse(segmentSplit[i], NumberStyles.HexNumber);
                    if (rleCount <= 1)
                    {
                        log.LogError(string.Format("Invalid RLE segment {0} in token {1} in NSS file {2}", segmentSplit[i], token, nss.SourceFile), null);
                        return Array.Empty<byte>();
                    }
                    // One byte is already counted in the previous data segment
                    outputSize += rleCount - 1;
                }
                else
                {
                    // Data segment
                    if (!int.IsEvenInteger(segmentSplit[i].Length))
                    {
                        log.LogError(string.Format("Binary data segment was an odd number of characters in token {1} in NSS file {2}", segmentSplit[i], token, nss.SourceFile), null);
                        return Array.Empty<byte>();
                    }
                    else
                    {
                        outputSize += (int)(segmentSplit[i].Length / 2);
                    }
                }
            }

            byte[] output = new byte[outputSize];
            byte[]? data = null;
            int index = 0;
            for (int i = 0; i < segmentSplit.Length; i++)
            {
                if (int.IsOddInteger(i))
                {
                    // RLE segment
                    if (data == null || data.Length == 0)
                    {
                        log.LogError(string.Format("Found RLE segment with no previous valid data segment in token {1} in NSS file {2}", segmentSplit[i], token, nss.SourceFile), null);
                        return Array.Empty<byte>();
                    }
                    int rleCount = int.Parse(segmentSplit[i], NumberStyles.HexNumber);
                    // One byte is already counted in the previous data segment
                    for (int j = 0; j < rleCount - 1; j++)
                    {
                        output[index] = data[data.Length - 1];
                        index++;
                    }
                }
                else
                {
                    // Data segment
                    data = Convert.FromHexString(segmentSplit[i]);
                    for (int j = 0; j < data.Length; j++)
                    {
                        output[index] = data[j];
                        index++;
                    }
                }
            }

            if (index != output.Length)
            {
                log.LogError(string.Format("Number of bytes written to output did not match output buffer size in token {0} in NSS file {1}", token, nss.SourceFile), null);
                return Array.Empty<byte>();
            }

            return output;
        }
    }
}
