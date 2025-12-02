using System.Globalization;

namespace OneBrc;

/// <summary>
/// Naive implementation of the 1BRC challenge.
/// Reads temperature measurements from a file and calculates min, mean, max per station.
/// This is the reference implementation - engineers should create optimized implementations
/// of <see cref="IWeatherProcessor"/> (e.g., using memory-mapped files, parallel processing, SIMD, etc.)
/// </summary>
public class NaiveWeatherProcessor : IWeatherProcessor
{
    /// <inheritdoc />
    public IEnumerable<StationData> Process(string filePath)
    {
        var stations = new Dictionary<string, StationData>();

        foreach (var line in File.ReadLines(filePath))
        {
            ProcessLine(line, stations);
        }

        return stations.Values.OrderBy(s => s.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Processes a single measurement line and updates station data.
    /// </summary>
    protected void ProcessLine(string line, Dictionary<string, StationData> stations)
    {
        var separatorIndex = line.IndexOf(';');
        if (separatorIndex == -1) return;

        var name = line.Substring(0, separatorIndex);
        var tempStr = line.Substring(separatorIndex + 1);
        var temp = double.Parse(tempStr, CultureInfo.InvariantCulture);

        if (stations.TryGetValue(name, out var data))
        {
            data.Min = Math.Min(data.Min, temp);
            data.Max = Math.Max(data.Max, temp);
            data.Sum += temp;
            data.Count++;
        }
        else
        {
            stations[name] = new StationData
            {
                Name = name,
                Min = temp,
                Max = temp,
                Sum = temp,
                Count = 1
            };
        }
    }
}
