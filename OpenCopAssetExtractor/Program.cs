using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenCopAssetExtractor
{
    static class Program
    {
        static void Main(string[] args)
        {
            string sourcePath = Path.Combine($"Data/Source");
            string destinationPath = Path.Combine($"Data/Extract");
            DirectoryInfo dir = new DirectoryInfo(sourcePath);
            FileInfo[] files = dir.GetFiles("*");
            
            foreach(FileInfo file in files )
            {
                var archive = File.ReadAllBytes($"{sourcePath}/{file.Name}");
                byte[] fileStartMarker =Encoding.ASCII.GetBytes("RDHS");
                byte[] fileEndMarker = Encoding.ASCII.GetBytes("COHS<");
                var fileStartPositions = FindFileStartPositions(archive, fileStartMarker).ToArray();

                Console.WriteLine($"Found {fileStartPositions.Count()} files");

                Directory.CreateDirectory($"{destinationPath}/{file.Name}");

                int fileCount = 0;
                foreach (var fileStartPosition in fileStartPositions)
                {
                    fileCount++;
                    var fileEndPosition = FindNextFileEndPosition(fileStartPosition, archive, fileEndMarker);
                    Console.WriteLine($"Extracting bytes start: {fileStartPosition} end: {fileEndPosition} for {file.Name}_{fileCount}");
                    var fileData = archive.Skip(fileStartPosition).Take(fileEndPosition - fileStartPosition).ToArray();
                
                    byte[] headerEndMarker = Encoding.ASCII.GetBytes("TADS");
                    var fileDataWithOutHeader = RemoveHeader(headerEndMarker, fileData).ToArray();
                
                    var pass1 = CleanFile(Encoding.ASCII.GetBytes("TADS"),fileDataWithOutHeader).ToArray();
                    var pass2 = CleanFile(StringToByteArray("1000000000000000000000"),pass1).ToArray();
                    var pass3 = CleanFile(Encoding.ASCII.GetBytes("COHS"),pass2).ToArray();

                    File.WriteAllBytes($"{destinationPath}/{file.Name}/{file.Name}_{fileCount}",pass3);
                }
            }
            
           
        }
        
        private static byte[] StringToByteArray(string hex) {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
        
        private static IEnumerable<int> FindFileStartPositions(byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    yield return i + (pattern.Length);
                }
            }
        }
        
        private static int FindNextFileEndPosition(int startPosition, byte[] source, byte[] pattern)
        {
            int endPosition = 0;
            for (int i = startPosition; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    endPosition = i;
                    break;
                }
            }
            return endPosition;
        }
        
        private static IEnumerable<byte> RemoveHeader(byte[] endHeaderPattern, byte[] source)
        {
            var endHeaderPosition = FindNextFileEndPosition(0, source, endHeaderPattern) + endHeaderPattern.Length;
            return source.Skip(endHeaderPosition);
        }

        private static IEnumerable<byte> CleanFile(byte[] offensivePattern, byte[] source)
        {
                int cleanIndex = 0;
                byte[] cleanedFile = new byte[source.Length];
                for (int sourceIndex = 0; sourceIndex < source.Length; sourceIndex++)
                {
                    if (source.Skip(sourceIndex).Take(offensivePattern.Length).SequenceEqual(offensivePattern))
                    {
                        sourceIndex += offensivePattern.Length;
                    }
                    else
                    {
                        cleanedFile[cleanIndex] = source[sourceIndex];
                        cleanIndex++;
                    }
                }
                return cleanedFile;
        }
    }
}