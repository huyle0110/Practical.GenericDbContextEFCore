using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Shipping company lookup table.
/// </summary>
[Table("ShipMethod", Schema = "Purchasing")]
[Index("Name", Name = "AK_ShipMethod_Name", IsUnique = true)]
[Index("rowguid", Name = "AK_ShipMethod_rowguid", IsUnique = true)]
public partial class ShipMethod
{
    /// <summary>
    /// Primary key for ShipMethod records.
    /// </summary>
    [Key]
    public int ShipMethodID { get; set; }

    /// <summary>
    /// Shipping company name.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Minimum shipping charge.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal ShipBase { get; set; }

    /// <summary>
    /// Shipping charge per pound.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal ShipRate { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("ShipMethod")]
    public virtual ICollection<PurchaseOrderHeader> PurchaseOrderHeader { get; set; } = new List<PurchaseOrderHeader>();

    [InverseProperty("ShipMethod")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeader { get; set; } = new List<SalesOrderHeader>();
}
