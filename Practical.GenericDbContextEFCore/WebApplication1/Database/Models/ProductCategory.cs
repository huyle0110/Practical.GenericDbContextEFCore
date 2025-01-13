using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// High-level product categorization.
/// </summary>
[Table("ProductCategory", Schema = "Production")]
[Index("Name", Name = "AK_ProductCategory_Name", IsUnique = true)]
[Index("rowguid", Name = "AK_ProductCategory_rowguid", IsUnique = true)]
public partial class ProductCategory
{
    /// <summary>
    /// Primary key for ProductCategory records.
    /// </summary>
    [Key]
    public int ProductCategoryID { get; set; }

    /// <summary>
    /// Category description.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("ProductCategory")]
    public virtual ICollection<ProductSubcategory> ProductSubcategory { get; set; } = new List<ProductSubcategory>();
}
