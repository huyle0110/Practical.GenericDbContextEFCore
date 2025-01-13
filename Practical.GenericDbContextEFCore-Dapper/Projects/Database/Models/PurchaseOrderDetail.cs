using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Individual products associated with a specific purchase order. See PurchaseOrderHeader.
/// </summary>
[PrimaryKey("PurchaseOrderID", "PurchaseOrderDetailID")]
[Table("PurchaseOrderDetail", Schema = "Purchasing")]
[Index("ProductID", Name = "IX_PurchaseOrderDetail_ProductID")]
public partial class PurchaseOrderDetail
{
    /// <summary>
    /// Primary key. Foreign key to PurchaseOrderHeader.PurchaseOrderID.
    /// </summary>
    [Key]
    public int PurchaseOrderID { get; set; }

    /// <summary>
    /// Primary key. One line number per purchased product.
    /// </summary>
    [Key]
    public int PurchaseOrderDetailID { get; set; }

    /// <summary>
    /// Date the product is expected to be received.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public short OrderQty { get; set; }

    /// <summary>
    /// Product identification number. Foreign key to Product.ProductID.
    /// </summary>
    public int ProductID { get; set; }

    /// <summary>
    /// Vendor&apos;s selling price of a single product.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Per product subtotal. Computed as OrderQty * UnitPrice.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Quantity actually received from the vendor.
    /// </summary>
    [Column(TypeName = "decimal(8, 2)")]
    public decimal ReceivedQty { get; set; }

    /// <summary>
    /// Quantity rejected during inspection.
    /// </summary>
    [Column(TypeName = "decimal(8, 2)")]
    public decimal RejectedQty { get; set; }

    /// <summary>
    /// Quantity accepted into inventory. Computed as ReceivedQty - RejectedQty.
    /// </summary>
    [Column(TypeName = "decimal(9, 2)")]
    public decimal StockedQty { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("ProductID")]
    [InverseProperty("PurchaseOrderDetail")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("PurchaseOrderID")]
    [InverseProperty("PurchaseOrderDetail")]
    public virtual PurchaseOrderHeader PurchaseOrder { get; set; } = null!;
}
