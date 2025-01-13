using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Source of the ID that connects vendors, customers, and employees with address and contact information.
/// </summary>
[Table("BusinessEntity", Schema = "Person")]
[Index("rowguid", Name = "AK_BusinessEntity_rowguid", IsUnique = true)]
public partial class BusinessEntity
{
    /// <summary>
    /// Primary key for all customers, vendors, and employees.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("BusinessEntity")]
    public virtual ICollection<BusinessEntityAddress> BusinessEntityAddress { get; set; } = new List<BusinessEntityAddress>();

    [InverseProperty("BusinessEntity")]
    public virtual ICollection<BusinessEntityContact> BusinessEntityContact { get; set; } = new List<BusinessEntityContact>();

    [InverseProperty("BusinessEntity")]
    public virtual Person? Person { get; set; }

    [InverseProperty("BusinessEntity")]
    public virtual Store? Store { get; set; }

    [InverseProperty("BusinessEntity")]
    public virtual Vendor? Vendor { get; set; }
}
