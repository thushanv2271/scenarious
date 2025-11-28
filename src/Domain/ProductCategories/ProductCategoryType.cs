using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ProductCategories;

/// <summary>
/// Defines the valid types for product categories
/// </summary>
public enum ProductCategoryType
{
    /// <summary>
    /// Probability of Default
    /// </summary>
    PD,

    /// <summary>
    /// Loss Given Default
    /// </summary>
    LGD
}