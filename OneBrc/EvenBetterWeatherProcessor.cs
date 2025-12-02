using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace OneBrc
{
    /// <summary>
    /// Streaming, span-based implementation of IWeatherProcessor.
    /// Optimised for very large input files (hundreds of millions / billions of rows).
    /// </summary>
    public sealed class EvenBetterWeatherProcessor : IWeatherProcessor
    {
        // Internal stats: everything in "tenths of degrees" to avoid per-row floating point.
        private struct RunningStats
        {
            public int Min;       // in tenths
            public int Max;       // in tenths
            public long Sum;      // sum of tenths
            public long Count;    // number of measurements

            public void Add(int valueTenths)
            {
                if (Count == 0)
                {
                    Min = Max = valueTenths;
                    Sum = valueTenths;
                    Count = 1;
                    return;
                }

                if (valueTenths < Min) Min = valueTenths;
                if (valueTenths > Max) Max = valueTenths;
                Sum += valueTenths;
                Count++;
            }

            public readonly double GetMinAsDouble() => Min / 10.0;
            public readonly double GetMaxAsDouble() => Max / 10.0;
            public readonly double GetMeanAsDouble() => Count == 0 ? double.NaN : (Sum / 10.0) / Count;
        }

        public IEnumerable<StationData> Process(string filePath)
        {
            // If you know approx station count, pre-seed capacity for fewer resizes.
            var statsByStation = new Dictionary<string, RunningStats>(capacity: 32_768, comparer: StringComparer.Ordinal);

            // Biggish buffer & SequentialScan hint avoid thrashing I/O.
            using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1 << 20,                // 1 MB
                options: FileOptions.SequentialScan);

            using var reader = new StreamReader(fs);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ProcessLine(line.AsSpan(), statsByStation);
            }

            // Build final result, sorted alphabetically by station name
            var result = new List<StationData>(statsByStation.Count);

            foreach (var (name, rs) in statsByStation)
            {
                result.Add(new StationData
                {
                    Name = name,
                    Min = rs.GetMinAsDouble(),
                    Max = rs.GetMaxAsDouble(),
                    Sum = rs.Sum,
                    Count = rs.Count
                }
                );
            }

            // sorted alphabetically
            return [.. result.OrderBy(sd => sd.Name, StringComparer.Ordinal)]; // materialise so caller isn't re-sorting on each enumeration
        }

        private static void ProcessLine(ReadOnlySpan<char> line, Dictionary<string, RunningStats> statsByStation)
        {
            // Find the ';' separator
            int sepIndex = line.IndexOf(';');
            if (sepIndex <= 0 || sepIndex >= line.Length - 1)
            {
                // Malformed line; ignore or throw depending on your needs.
                return;
            }

            var nameSpan = line[..sepIndex];
            var tempSpan = line[(sepIndex + 1)..];

            // Parse temperature as an int in tenths of a degree (e.g. "12.3" -> 123)
            int tempTenths = ParseTemperatureToTenths(tempSpan);

            // Allocate a string once per distinct station.
            // string.Intern here is optional; it ensures deduplication across the whole AppDomain,
            // which can be useful for 1BRC-style datasets with many repeated names.
            string stationName = string.Intern(nameSpan.ToString());

            // Use CollectionsMarshal to avoid double-lookup & struct copying overhead
            ref RunningStats entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
                statsByStation,
                stationName,
                out bool exists);

            if (!exists)
            {
                entry = default;
            }

            entry.Add(tempTenths);
        }

        /// <summary>
        /// Fast parser for temperatures with exactly one decimal place, e.g. "-12.3", "4.0", "0.1".
        /// Returns an int representing tenths of a degree (e.g. "12.3" -> 123).
        /// </summary>
        private static int ParseTemperatureToTenths(ReadOnlySpan<char> span)
        {
            // Format assumed: optional '-' + digits + '.' + 1 digit
            // Examples: "12.3", "-5.1", "0.0"
            int sign = 1;
            int i = 0;

            if (span[0] == '-')
            {
                sign = -1;
                i = 1;
            }

            int value = 0;

            for (; i < span.Length; i++)
            {
                char c = span[i];
                if (c == '.')
                {
                    // skip the dot; the remaining digits logically include the tenths
                    continue;
                }

                value = (value * 10) + (c - '0');
            }

            return sign * value;
        }
    }
}
