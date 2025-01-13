using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Lookup table of customer purchase reasons.
/// </summary>
[Table("SalesReason", Schema = "Sales")]
public partial class SalesReason
{
    /// <summary>
    /// Primary key for SalesReason records.
    /// </summary>
    [Key]
    public int SalesReasonID { get; set; }

    /// <summary>
    /// Sales reason description.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Category the sales reason belongs to.
    /// </summary>
    [StringLength(50)]
    public string ReasonType { get; set; } = null!;

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("SalesReason")]
    public virtual ICollection<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReason { get; set; } = new List<SalesOrderHeaderSalesReason>();
}
