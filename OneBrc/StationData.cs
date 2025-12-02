namespace OneBrc;

/// <summary>
/// Holds aggregated temperature data for a weather station.
/// </summary>
public class StationData
{
    /// <summary>
    /// The name of the weather station.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The minimum temperature recorded.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// The maximum temperature recorded.
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// The sum of all temperatures (used to calculate mean).
    /// </summary>
    public double Sum { get; set; }

    /// <summary>
    /// The number of measurements recorded.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// The mean (average) temperature.
    /// </summary>
    public double Mean => Count > 0 ? Sum / Count : 0;
}
