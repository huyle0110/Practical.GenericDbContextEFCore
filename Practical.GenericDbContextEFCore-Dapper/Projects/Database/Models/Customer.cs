using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Current customer information. Also see the Person and Store tables.
/// </summary>
[Table("Customer", Schema = "Sales")]
[Index("AccountNumber", Name = "AK_Customer_AccountNumber", IsUnique = true)]
[Index("rowguid", Name = "AK_Customer_rowguid", IsUnique = true)]
[Index("TerritoryID", Name = "IX_Customer_TerritoryID")]
public partial class Customer
{
    /// <summary>
    /// Primary key.
    /// </summary>
    [Key]
    public int CustomerID { get; set; }

    /// <summary>
    /// Foreign key to Person.BusinessEntityID
    /// </summary>
    public int? PersonID { get; set; }

    /// <summary>
    /// Foreign key to Store.BusinessEntityID
    /// </summary>
    public int? StoreID { get; set; }

    /// <summary>
    /// ID of the territory in which the customer is located. Foreign key to SalesTerritory.SalesTerritoryID.
    /// </summary>
    public int? TerritoryID { get; set; }

    /// <summary>
    /// Unique number identifying the customer assigned by the accounting system.
    /// </summary>
    [StringLength(10)]
    [Unicode(false)]
    public string AccountNumber { get; set; } = null!;

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("PersonID")]
    [InverseProperty("Customer")]
    public virtual Person? Person { get; set; }

    [InverseProperty("Customer")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeader { get; set; } = new List<SalesOrderHeader>();

    [ForeignKey("StoreID")]
    [InverseProperty("Customer")]
    public virtual Store? Store { get; set; }

    [ForeignKey("TerritoryID")]
    [InverseProperty("Customer")]
    public virtual SalesTerritory? Territory { get; set; }
}
