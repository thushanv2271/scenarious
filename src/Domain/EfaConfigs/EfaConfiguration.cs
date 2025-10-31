using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.EfaConfigs;
public sealed class EfaConfiguration : Entity
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public decimal EfaRate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
}
