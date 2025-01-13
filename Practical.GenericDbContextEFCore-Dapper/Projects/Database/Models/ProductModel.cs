using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Product model classification.
/// </summary>
[Table("ProductModel", Schema = "Production")]
[Index("Name", Name = "AK_ProductModel_Name", IsUnique = true)]
[Index("rowguid", Name = "AK_ProductModel_rowguid", IsUnique = true)]
[Index("CatalogDescription", Name = "PXML_ProductModel_CatalogDescription")]
[Index("Instructions", Name = "PXML_ProductModel_Instructions")]
public partial class ProductModel
{
    /// <summary>
    /// Primary key for ProductModel records.
    /// </summary>
    [Key]
    public int ProductModelID { get; set; }

    /// <summary>
    /// Product model description.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Detailed product catalog information in xml format.
    /// </summary>
    [Column(TypeName = "xml")]
    public string? CatalogDescription { get; set; }

    /// <summary>
    /// Manufacturing instructions in xml format.
    /// </summary>
    [Column(TypeName = "xml")]
    public string? Instructions { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("ProductModel")]
    public virtual ICollection<Product> Product { get; set; } = new List<Product>();

    [InverseProperty("ProductModel")]
    public virtual ICollection<ProductModelIllustration> ProductModelIllustration { get; set; } = new List<ProductModelIllustration>();

    [InverseProperty("ProductModel")]
    public virtual ICollection<ProductModelProductDescriptionCulture> ProductModelProductDescriptionCulture { get; set; } = new List<ProductModelProductDescriptionCulture>();
}
