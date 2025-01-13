using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Sales representative current information.
/// </summary>
[Table("SalesPerson", Schema = "Sales")]
[Index("rowguid", Name = "AK_SalesPerson_rowguid", IsUnique = true)]
public partial class SalesPerson
{
    /// <summary>
    /// Primary key for SalesPerson records. Foreign key to Employee.BusinessEntityID
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Territory currently assigned to. Foreign key to SalesTerritory.SalesTerritoryID.
    /// </summary>
    public int? TerritoryID { get; set; }

    /// <summary>
    /// Projected yearly sales.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal? SalesQuota { get; set; }

    /// <summary>
    /// Bonus due if quota is met.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal Bonus { get; set; }

    /// <summary>
    /// Commision percent received per sale.
    /// </summary>
    [Column(TypeName = "smallmoney")]
    public decimal CommissionPct { get; set; }

    /// <summary>
    /// Sales total year to date.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal SalesYTD { get; set; }

    /// <summary>
    /// Sales total of previous year.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal SalesLastYear { get; set; }

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
    [InverseProperty("SalesPerson")]
    public virtual Employee BusinessEntity { get; set; } = null!;

    [InverseProperty("SalesPerson")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeader { get; set; } = new List<SalesOrderHeader>();

    [InverseProperty("BusinessEntity")]
    public virtual ICollection<SalesPersonQuotaHistory> SalesPersonQuotaHistory { get; set; } = new List<SalesPersonQuotaHistory>();

    [InverseProperty("BusinessEntity")]
    public virtual ICollection<SalesTerritoryHistory> SalesTerritoryHistory { get; set; } = new List<SalesTerritoryHistory>();

    [InverseProperty("SalesPerson")]
    public virtual ICollection<Store> Store { get; set; } = new List<Store>();

    [ForeignKey("TerritoryID")]
    [InverseProperty("SalesPerson")]
    public virtual SalesTerritory? Territory { get; set; }
}
