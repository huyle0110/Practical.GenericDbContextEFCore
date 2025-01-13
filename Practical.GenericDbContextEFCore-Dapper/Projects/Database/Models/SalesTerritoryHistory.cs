using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Sales representative transfers to other sales territories.
/// </summary>
[PrimaryKey("BusinessEntityID", "StartDate", "TerritoryID")]
[Table("SalesTerritoryHistory", Schema = "Sales")]
[Index("rowguid", Name = "AK_SalesTerritoryHistory_rowguid", IsUnique = true)]
public partial class SalesTerritoryHistory
{
    /// <summary>
    /// Primary key. The sales rep.  Foreign key to SalesPerson.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Primary key. Territory identification number. Foreign key to SalesTerritory.SalesTerritoryID.
    /// </summary>
    [Key]
    public int TerritoryID { get; set; }

    /// <summary>
    /// Primary key. Date the sales representive started work in the territory.
    /// </summary>
    [Key]
    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date the sales representative left work in the territory.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime? EndDate { get; set; }

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
    [InverseProperty("SalesTerritoryHistory")]
    public virtual SalesPerson BusinessEntity { get; set; } = null!;

    [ForeignKey("TerritoryID")]
    [InverseProperty("SalesTerritoryHistory")]
    public virtual SalesTerritory Territory { get; set; } = null!;
}
