using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Changes in the list price of a product over time.
/// </summary>
[PrimaryKey("ProductID", "StartDate")]
[Table("ProductListPriceHistory", Schema = "Production")]
public partial class ProductListPriceHistory
{
    /// <summary>
    /// Product identification number. Foreign key to Product.ProductID
    /// </summary>
    [Key]
    public int ProductID { get; set; }

    /// <summary>
    /// List price start date.
    /// </summary>
    [Key]
    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// List price end date
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Product list price.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal ListPrice { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("ProductID")]
    [InverseProperty("ProductListPriceHistory")]
    public virtual Product Product { get; set; } = null!;
}
