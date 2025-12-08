using SharedKernel;

namespace Domain.Configurations;

public class AgeBucketConfiguration : Entity
{
    public Guid Id { get; set; }
    public AgeBucketConfigurationData ConfigurationData { get; set; } = new();
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
}

/// <summary>
/// Contains all configuration data stored as a single JSON object
/// </summary>
public sealed class AgeBucketConfigurationData
{
    public ICollection<DatePassedDueBasedConfiguration>? DatePassedDueBasedConfigurations { get; set; } = [];
    public ICollection<ReschedulesAndRestructuredBasedConfiguration>? ReschedulesAndRestructuredBasedConfigurations { get; set; } = [];
    public ICollection<IndustryBasedConfiguration>? IndustryBasedConfigurations { get; set; } = [];
}
