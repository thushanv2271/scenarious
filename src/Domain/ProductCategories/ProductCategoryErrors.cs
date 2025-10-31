using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.ProductCategories;
public static class ProductCategoryErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "ProductCategory.NotFound",
        $"The product category with ID '{id}' was not found");

    public static Error NameNotUnique => Error.Conflict(
        "ProductCategory.NameNotUnique",
        "The product category name must be unique");
}
