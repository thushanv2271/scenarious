using SharedKernel;

namespace Domain.Exports;

public sealed class ExportAudit : Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime ExportedAt { get; private set; }
    public Guid ExportedBy { get; private set; } // UserId
    public string Url { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;

    private ExportAudit() { } // EF Core constructor

    public ExportAudit(Guid exportedBy, string file, string category)
    {
        ExportedAt = DateTime.UtcNow;
        ExportedBy = exportedBy;
        Url = file;
        Category = category;
    }
}
