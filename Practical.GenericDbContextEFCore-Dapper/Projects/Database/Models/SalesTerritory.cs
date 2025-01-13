using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Sales territory lookup table.
/// </summary>
[Table("SalesTerritory", Schema = "Sales")]
[Index("Name", Name = "AK_SalesTerritory_Name", IsUnique = true)]
[Index("rowguid", Name = "AK_SalesTerritory_rowguid", IsUnique = true)]
public partial class SalesTerritory
{
    /// <summary>
    /// Primary key for SalesTerritory records.
    /// </summary>
    [Key]
    public int TerritoryID { get; set; }

    /// <summary>
    /// Sales territory description
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// ISO standard country or region code. Foreign key to CountryRegion.CountryRegionCode. 
    /// </summary>
    [StringLength(3)]
    public string CountryRegionCode { get; set; } = null!;

    /// <summary>
    /// Geographic area to which the sales territory belong.
    /// </summary>
    [StringLength(50)]
    public string Group { get; set; } = null!;

    /// <summary>
    /// Sales in the territory year to date.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal SalesYTD { get; set; }

    /// <summary>
    /// Sales in the territory the previous year.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal SalesLastYear { get; set; }

    /// <summary>
    /// Business costs in the territory year to date.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal CostYTD { get; set; }

    /// <summary>
    /// Business costs in the territory the previous year.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal CostLastYear { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("CountryRegionCode")]
    [InverseProperty("SalesTerritory")]
    public virtual CountryRegion CountryRegionCodeNavigation { get; set; } = null!;

    [InverseProperty("Territory")]
    public virtual ICollection<Customer> Customer { get; set; } = new List<Customer>();

    [InverseProperty("Territory")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeader { get; set; } = new List<SalesOrderHeader>();

    [InverseProperty("Territory")]
    public virtual ICollection<SalesPerson> SalesPerson { get; set; } = new List<SalesPerson>();

    [InverseProperty("Territory")]
    public virtual ICollection<SalesTerritoryHistory> SalesTerritoryHistory { get; set; } = new List<SalesTerritoryHistory>();

    [InverseProperty("Territory")]
    public virtual ICollection<StateProvince> StateProvince { get; set; } = new List<StateProvince>();
}
