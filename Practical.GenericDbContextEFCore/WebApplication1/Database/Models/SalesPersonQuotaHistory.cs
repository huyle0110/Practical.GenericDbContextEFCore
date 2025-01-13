using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Sales performance tracking.
/// </summary>
[PrimaryKey("BusinessEntityID", "QuotaDate")]
[Table("SalesPersonQuotaHistory", Schema = "Sales")]
[Index("rowguid", Name = "AK_SalesPersonQuotaHistory_rowguid", IsUnique = true)]
public partial class SalesPersonQuotaHistory
{
    /// <summary>
    /// Sales person identification number. Foreign key to SalesPerson.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Sales quota date.
    /// </summary>
    [Key]
    [Column(TypeName = "datetime")]
    public DateTime QuotaDate { get; set; }

    /// <summary>
    /// Sales quota amount.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal SalesQuota { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("BusinessEntityID")]
    [InverseProperty("SalesPersonQuotaHistory")]
    public virtual SalesPerson BusinessEntity { get; set; } = null!;
}
