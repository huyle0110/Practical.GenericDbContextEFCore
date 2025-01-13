using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Street address information for customers, employees, and vendors.
/// </summary>
[Table("Address", Schema = "Person")]
[Index("rowguid", Name = "AK_Address_rowguid", IsUnique = true)]
[Index("AddressLine1", "AddressLine2", "City", "StateProvinceID", "PostalCode", Name = "IX_Address_AddressLine1_AddressLine2_City_StateProvinceID_PostalCode", IsUnique = true)]
[Index("StateProvinceID", Name = "IX_Address_StateProvinceID")]
public partial class Address
{
    /// <summary>
    /// Primary key for Address records.
    /// </summary>
    [Key]
    public int AddressID { get; set; }

    /// <summary>
    /// First street address line.
    /// </summary>
    [StringLength(60)]
    public string AddressLine1 { get; set; } = null!;

    /// <summary>
    /// Second street address line.
    /// </summary>
    [StringLength(60)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Name of the city.
    /// </summary>
    [StringLength(30)]
    public string City { get; set; } = null!;

    /// <summary>
    /// Unique identification number for the state or province. Foreign key to StateProvince table.
    /// </summary>
    public int StateProvinceID { get; set; }

    /// <summary>
    /// Postal code for the street address.
    /// </summary>
    [StringLength(15)]
    public string PostalCode { get; set; } = null!;

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("Address")]
    public virtual ICollection<BusinessEntityAddress> BusinessEntityAddress { get; set; } = new List<BusinessEntityAddress>();

    [InverseProperty("BillToAddress")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeaderBillToAddress { get; set; } = new List<SalesOrderHeader>();

    [InverseProperty("ShipToAddress")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeaderShipToAddress { get; set; } = new List<SalesOrderHeader>();

    [ForeignKey("StateProvinceID")]
    [InverseProperty("Address")]
    public virtual StateProvince StateProvince { get; set; } = null!;
}
