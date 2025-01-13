using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Cross-reference table mapping customers, vendors, and employees to their addresses.
/// </summary>
[PrimaryKey("BusinessEntityID", "AddressID", "AddressTypeID")]
[Table("BusinessEntityAddress", Schema = "Person")]
[Index("rowguid", Name = "AK_BusinessEntityAddress_rowguid", IsUnique = true)]
[Index("AddressID", Name = "IX_BusinessEntityAddress_AddressID")]
[Index("AddressTypeID", Name = "IX_BusinessEntityAddress_AddressTypeID")]
public partial class BusinessEntityAddress
{
    /// <summary>
    /// Primary key. Foreign key to BusinessEntity.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Primary key. Foreign key to Address.AddressID.
    /// </summary>
    [Key]
    public int AddressID { get; set; }

    /// <summary>
    /// Primary key. Foreign key to AddressType.AddressTypeID.
    /// </summary>
    [Key]
    public int AddressTypeID { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("AddressID")]
    [InverseProperty("BusinessEntityAddress")]
    public virtual Address Address { get; set; } = null!;

    [ForeignKey("AddressTypeID")]
    [InverseProperty("BusinessEntityAddress")]
    public virtual AddressType AddressType { get; set; } = null!;

    [ForeignKey("BusinessEntityID")]
    [InverseProperty("BusinessEntityAddress")]
    public virtual BusinessEntity BusinessEntity { get; set; } = null!;
}
