using System;
using System.Collections.Generic;
using System.Text;

namespace OneBrc
{
    internal class BestWeatherProcessor : IWeatherProcessor
    {
        public IEnumerable<StationData> Process(string filePath)
        {
            // Biggish buffer & SequentialScan hint avoid thrashing I/O.
            using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1 << 20,                // 1 MB
                options: FileOptions.SequentialScan);

            using var reader = new StreamReader(fs);

            Dictionary<string, List<string>> groupedLines = new();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] lineArr = line.Split(';');
                
                if (groupedLines.ContainsKey(lineArr[0]))
                {

                }
                else
                {

                }
            }
        }
    }
}
