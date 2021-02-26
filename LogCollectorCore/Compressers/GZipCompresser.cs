using System.IO;
using System.IO.Compression;

namespace LogCollectorCore.Compresser
{
    public class GZipCompresser: ILogCompresser
    {
        public string Identifier => "gzip";

        public byte[] Decompress(byte[] content)
        {
            using (var origin = new MemoryStream(content))
            {
                using (var decompressed = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(origin, CompressionMode.Decompress))
                    {
                        gzipStream.CopyTo(decompressed);
                    }

                    // call after GZipStream Dispose.
                    return decompressed.ToArray();
                }
            }
        }
    }
}
