using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Cross-reference table mapping product descriptions and the language the description is written in.
/// </summary>
[PrimaryKey("ProductModelID", "ProductDescriptionID", "CultureID")]
[Table("ProductModelProductDescriptionCulture", Schema = "Production")]
public partial class ProductModelProductDescriptionCulture
{
    /// <summary>
    /// Primary key. Foreign key to ProductModel.ProductModelID.
    /// </summary>
    [Key]
    public int ProductModelID { get; set; }

    /// <summary>
    /// Primary key. Foreign key to ProductDescription.ProductDescriptionID.
    /// </summary>
    [Key]
    public int ProductDescriptionID { get; set; }

    /// <summary>
    /// Culture identification number. Foreign key to Culture.CultureID.
    /// </summary>
    [Key]
    [StringLength(6)]
    public string CultureID { get; set; } = null!;

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("CultureID")]
    [InverseProperty("ProductModelProductDescriptionCulture")]
    public virtual Culture Culture { get; set; } = null!;

    [ForeignKey("ProductDescriptionID")]
    [InverseProperty("ProductModelProductDescriptionCulture")]
    public virtual ProductDescription ProductDescription { get; set; } = null!;

    [ForeignKey("ProductModelID")]
    [InverseProperty("ProductModelProductDescriptionCulture")]
    public virtual ProductModel ProductModel { get; set; } = null!;
}
