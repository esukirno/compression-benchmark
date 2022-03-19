using K4os.Compression.LZ4.Streams;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace CompressionBenchmark.Console
{
    public enum Encoding
    {
        GZip,
        GZipFast,
        LZ4,
        DeflateFast
    }

    class Program
    {
        static void Main(string[] args)
        {
            var filePath2MB = @"..\..\..\sample-2mb-text-file.txt";
            var filePath20MB = @"..\..\..\sample-20mb-text-file.txt";
            var filePath40MB = @"..\..\..\sample-40mb-text-file.txt";

            CompressionStats(Encoding.GZip, filePath2MB);
            CompressionStats(Encoding.GZipFast, filePath2MB);
            CompressionStats(Encoding.LZ4, filePath2MB);
            CompressionStats(Encoding.DeflateFast, filePath2MB);

            CompressionStats(Encoding.GZip, filePath20MB);
            CompressionStats(Encoding.GZipFast, filePath20MB);
            CompressionStats(Encoding.LZ4, filePath20MB);
            CompressionStats(Encoding.DeflateFast, filePath20MB);

            CompressionStats(Encoding.GZip, filePath40MB);
            CompressionStats(Encoding.GZipFast, filePath40MB);
            CompressionStats(Encoding.LZ4, filePath40MB);
            CompressionStats(Encoding.DeflateFast, filePath40MB);

        }

        private static Stream GetCompressionDestinationStream(Encoding encoding, string compressedFilePath)
        {
            switch(encoding)
            {
                case Encoding.GZip:
                    {
                        return new GZipStream(File.Create(compressedFilePath), CompressionMode.Compress);
                    }
                case Encoding.GZipFast:
                    {
                        return new GZipStream(File.Create(compressedFilePath), CompressionLevel.Fastest);
                    }
                case Encoding.LZ4:
                    {
                        return LZ4Stream.Encode(File.Create(compressedFilePath), K4os.Compression.LZ4.LZ4Level.L00_FAST);
                    }
                case Encoding.DeflateFast:
                    {
                        return new DeflateStream(File.Create(compressedFilePath), CompressionLevel.Fastest);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static Stream GetDecompressionSourceStream(Encoding encoding, string compressedFilePath)
        {
            switch (encoding)
            {
                case Encoding.GZip:
                case Encoding.GZipFast:
                    {
                        return new GZipStream(File.OpenRead(compressedFilePath), CompressionMode.Decompress);
                    }
                case Encoding.LZ4:
                    {
                        return LZ4Stream.Decode(File.OpenRead(compressedFilePath));
                    }
                case Encoding.DeflateFast:
                    {
                        return new DeflateStream(File.OpenRead(compressedFilePath), CompressionMode.Decompress);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static void CompressionStats(Encoding encoding, string filePath)
        {
            System.Console.WriteLine($"=== {encoding} {filePath} ===");

            long compressedByteCounts;
            long sourceByteCounts;
            double compressionRate;
            long compressionElapsedMs;
            long decompressionElapsedMs;

            string compressedFilePath = new FileInfo(filePath).Name + "." + encoding.ToString().ToLowerInvariant();
            string decompressedFilePath = compressedFilePath + ".orig";

            using (var destination = GetCompressionDestinationStream(encoding, compressedFilePath))
            using (var source = File.OpenRead(filePath))
            {
                var timer = Stopwatch.StartNew();

                source.CopyTo(destination);

                timer.Stop();

                compressionElapsedMs = timer.ElapsedMilliseconds;
            }

            using (var destination = File.Create(decompressedFilePath))
            using (var source = GetDecompressionSourceStream(encoding, compressedFilePath))
            {
                var timer = Stopwatch.StartNew();

                source.CopyTo(destination);

                timer.Stop();
                decompressionElapsedMs = timer.ElapsedMilliseconds;
            }

            if (!CompareByMD5(filePath, decompressedFilePath))
            {
                System.Console.WriteLine("ERROR file corrupted");
            }

            compressedByteCounts = new FileInfo(compressedFilePath).Length;
            sourceByteCounts = new FileInfo(filePath).Length;
            compressionRate = CalculateCompressionRate(sourceByteCounts, compressedByteCounts);

            System.Console.WriteLine($"Compression elapsed: {compressionElapsedMs} ms");
            System.Console.WriteLine($"Decompression elapsed: {decompressionElapsedMs} ms");
            System.Console.WriteLine($"Original byte counts: {sourceByteCounts}");
            System.Console.WriteLine($"Compressed byte counts: {compressedByteCounts}");
            System.Console.WriteLine($"Compression rate: {compressionRate}");
            System.Console.WriteLine();
        }

        private static double CalculateCompressionRate(long before, long after)
        {
            return (double) (before - after) / (double) before * 100;
        }

        private static bool CompareByMD5(string file1, string file2)
        {
            // Using the. NET built-in MD5 Library
            using (var md5 = MD5.Create())
            {
                byte[] one, two;
                using (var fs1 = File.Open(file1, FileMode.Open))
                {
                    // Read the file content with FileStream and calculate the HASH value
                    one = md5.ComputeHash(fs1);
                }
                using (var fs2 = File.Open(file2, FileMode.Open))
                {
                    // Read the file content with FileStream and calculate the HASH value
                    two = md5.ComputeHash(fs2);
                }
                // Converting MD5 results (byte arrays) into strings for comparison
                return BitConverter.ToString(one) == BitConverter.ToString(two);
            }
        }
    }
}
