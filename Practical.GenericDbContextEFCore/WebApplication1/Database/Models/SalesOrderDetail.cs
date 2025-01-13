using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Individual products associated with a specific sales order. See SalesOrderHeader.
/// </summary>
[PrimaryKey("SalesOrderID", "SalesOrderDetailID")]
[Table("SalesOrderDetail", Schema = "Sales")]
[Index("rowguid", Name = "AK_SalesOrderDetail_rowguid", IsUnique = true)]
[Index("ProductID", Name = "IX_SalesOrderDetail_ProductID")]
public partial class SalesOrderDetail
{
    /// <summary>
    /// Primary key. Foreign key to SalesOrderHeader.SalesOrderID.
    /// </summary>
    [Key]
    public int SalesOrderID { get; set; }

    /// <summary>
    /// Primary key. One incremental unique number per product sold.
    /// </summary>
    [Key]
    public int SalesOrderDetailID { get; set; }

    /// <summary>
    /// Shipment tracking number supplied by the shipper.
    /// </summary>
    [StringLength(25)]
    public string? CarrierTrackingNumber { get; set; }

    /// <summary>
    /// Quantity ordered per product.
    /// </summary>
    public short OrderQty { get; set; }

    /// <summary>
    /// Product sold to customer. Foreign key to Product.ProductID.
    /// </summary>
    public int ProductID { get; set; }

    /// <summary>
    /// Promotional code. Foreign key to SpecialOffer.SpecialOfferID.
    /// </summary>
    public int SpecialOfferID { get; set; }

    /// <summary>
    /// Selling price of a single product.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal UnitPriceDiscount { get; set; }

    /// <summary>
    /// Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.
    /// </summary>
    [Column(TypeName = "numeric(38, 6)")]
    public decimal LineTotal { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("SalesOrderID")]
    [InverseProperty("SalesOrderDetail")]
    public virtual SalesOrderHeader SalesOrder { get; set; } = null!;

    [ForeignKey("SpecialOfferID, ProductID")]
    [InverseProperty("SalesOrderDetail")]
    public virtual SpecialOfferProduct SpecialOfferProduct { get; set; } = null!;
}
