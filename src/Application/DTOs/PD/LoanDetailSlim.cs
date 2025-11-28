using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.PD;
public class LoanDetailSlim
{
    public string Period { get; set; } = default!;
    public string ProductCategory { get; set; } = default!;
    public string Segment { get; set; } = default!;
    public string CustomerNumber { get; set; } = default!;
    public string FacilityNumber { get; set; } = default!;
    public string FinalBucket { get; set; } = default!;
}

