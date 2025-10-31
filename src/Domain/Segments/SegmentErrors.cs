using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.Segments;
public static class SegmentErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Segment.NotFound",
        $"The segment with ID '{id}' was not found");

    public static Error NameNotUnique => Error.Conflict(
        "Segment.NameNotUnique",
        "The segment name must be unique within the product category");
}
