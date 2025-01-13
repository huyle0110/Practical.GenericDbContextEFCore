using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Changes in the cost of a product over time.
/// </summary>
[PrimaryKey("ProductID", "StartDate")]
[Table("ProductCostHistory", Schema = "Production")]
public partial class ProductCostHistory
{
    /// <summary>
    /// Product identification number. Foreign key to Product.ProductID
    /// </summary>
    [Key]
    public int ProductID { get; set; }

    /// <summary>
    /// Product cost start date.
    /// </summary>
    [Key]
    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Product cost end date.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Standard cost of the product.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal StandardCost { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("ProductID")]
    [InverseProperty("ProductCostHistory")]
    public virtual Product Product { get; set; } = null!;
}
