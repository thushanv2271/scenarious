namespace Domain.CollectiveImpairment;

public sealed class CollectiveImpairmentConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ParameterType Parameter { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
