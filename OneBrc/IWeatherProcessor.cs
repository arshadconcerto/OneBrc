namespace OneBrc;

/// <summary>
/// Interface for processing weather measurement data.
/// Implementations can use different strategies (streams, memory-mapped files, parallel processing, etc.)
/// </summary>
public interface IWeatherProcessor
{
    /// <summary>
    /// Processes a measurements file and returns station data.
    /// </summary>
    /// <param name="filePath">Path to the measurements file</param>
    /// <returns>Enumerable of station data with min, mean, max temperatures, alphabetically sorted by station name</returns>
    IEnumerable<StationData> Process(string filePath);
}
