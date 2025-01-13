using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Companies from whom Adventure Works Cycles purchases parts or other goods.
/// </summary>
[Table("Vendor", Schema = "Purchasing")]
[Index("AccountNumber", Name = "AK_Vendor_AccountNumber", IsUnique = true)]
public partial class Vendor
{
    /// <summary>
    /// Primary key for Vendor records.  Foreign key to BusinessEntity.BusinessEntityID
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Vendor account (identification) number.
    /// </summary>
    [StringLength(15)]
    public string AccountNumber { get; set; } = null!;

    /// <summary>
    /// Company name.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// 1 = Superior, 2 = Excellent, 3 = Above average, 4 = Average, 5 = Below average
    /// </summary>
    public byte CreditRating { get; set; }

    /// <summary>
    /// 0 = Do not use if another vendor is available. 1 = Preferred over other vendors supplying the same product.
    /// </summary>
    public bool PreferredVendorStatus { get; set; }

    /// <summary>
    /// 0 = Vendor no longer used. 1 = Vendor is actively used.
    /// </summary>
    public bool ActiveFlag { get; set; }

    /// <summary>
    /// Vendor URL.
    /// </summary>
    [StringLength(1024)]
    public string? PurchasingWebServiceURL { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("BusinessEntityID")]
    [InverseProperty("Vendor")]
    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    [InverseProperty("BusinessEntity")]
    public virtual ICollection<ProductVendor> ProductVendor { get; set; } = new List<ProductVendor>();

    [InverseProperty("Vendor")]
    public virtual ICollection<PurchaseOrderHeader> PurchaseOrderHeader { get; set; } = new List<PurchaseOrderHeader>();
}
