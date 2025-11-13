using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.ProductCategories;
using Domain.Scenarios;
using SharedKernel;

namespace Domain.Segments;
public sealed class Segment : Entity
{
    public Guid Id { get; set; }
    public Guid ProductCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ProductCategory ProductCategory { get; set; } = null!;
    public ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
}
