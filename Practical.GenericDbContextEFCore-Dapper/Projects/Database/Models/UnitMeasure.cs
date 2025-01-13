using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Unit of measure lookup table.
/// </summary>
[Table("UnitMeasure", Schema = "Production")]
[Index("Name", Name = "AK_UnitMeasure_Name", IsUnique = true)]
public partial class UnitMeasure
{
    /// <summary>
    /// Primary key.
    /// </summary>
    [Key]
    [StringLength(3)]
    public string UnitMeasureCode { get; set; } = null!;

    /// <summary>
    /// Unit of measure description.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("UnitMeasureCodeNavigation")]
    public virtual ICollection<BillOfMaterials> BillOfMaterials { get; set; } = new List<BillOfMaterials>();

    [InverseProperty("SizeUnitMeasureCodeNavigation")]
    public virtual ICollection<Product> ProductSizeUnitMeasureCodeNavigation { get; set; } = new List<Product>();

    [InverseProperty("UnitMeasureCodeNavigation")]
    public virtual ICollection<ProductVendor> ProductVendor { get; set; } = new List<ProductVendor>();

    [InverseProperty("WeightUnitMeasureCodeNavigation")]
    public virtual ICollection<Product> ProductWeightUnitMeasureCodeNavigation { get; set; } = new List<Product>();
}
