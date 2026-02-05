namespace MonetizationGateway.Configuration;

/// <summary>Options for the monthly usage summary background job.</summary>
public class MonthlyJobOptions
{
    public const string SectionName = "BackgroundJob";

    /// <summary>Interval in hours between job runs. Default: 24.</summary>
    public int SummaryJobIntervalHours { get; set; } = 24;
}
