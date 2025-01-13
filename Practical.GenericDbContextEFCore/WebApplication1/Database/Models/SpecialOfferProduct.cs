using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Cross-reference table mapping products to special offer discounts.
/// </summary>
[PrimaryKey("SpecialOfferID", "ProductID")]
[Table("SpecialOfferProduct", Schema = "Sales")]
[Index("rowguid", Name = "AK_SpecialOfferProduct_rowguid", IsUnique = true)]
[Index("ProductID", Name = "IX_SpecialOfferProduct_ProductID")]
public partial class SpecialOfferProduct
{
    /// <summary>
    /// Primary key for SpecialOfferProduct records.
    /// </summary>
    [Key]
    public int SpecialOfferID { get; set; }

    /// <summary>
    /// Product identification number. Foreign key to Product.ProductID.
    /// </summary>
    [Key]
    public int ProductID { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("ProductID")]
    [InverseProperty("SpecialOfferProduct")]
    public virtual Product Product { get; set; } = null!;

    [InverseProperty("SpecialOfferProduct")]
    public virtual ICollection<SalesOrderDetail> SalesOrderDetail { get; set; } = new List<SalesOrderDetail>();

    [ForeignKey("SpecialOfferID")]
    [InverseProperty("SpecialOfferProduct")]
    public virtual SpecialOffer SpecialOffer { get; set; } = null!;
}
