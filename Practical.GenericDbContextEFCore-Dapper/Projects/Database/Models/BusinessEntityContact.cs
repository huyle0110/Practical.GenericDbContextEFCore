using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Cross-reference table mapping stores, vendors, and employees to people
/// </summary>
[PrimaryKey("BusinessEntityID", "PersonID", "ContactTypeID")]
[Table("BusinessEntityContact", Schema = "Person")]
[Index("rowguid", Name = "AK_BusinessEntityContact_rowguid", IsUnique = true)]
[Index("ContactTypeID", Name = "IX_BusinessEntityContact_ContactTypeID")]
[Index("PersonID", Name = "IX_BusinessEntityContact_PersonID")]
public partial class BusinessEntityContact
{
    /// <summary>
    /// Primary key. Foreign key to BusinessEntity.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Primary key. Foreign key to Person.BusinessEntityID.
    /// </summary>
    [Key]
    public int PersonID { get; set; }

    /// <summary>
    /// Primary key.  Foreign key to ContactType.ContactTypeID.
    /// </summary>
    [Key]
    public int ContactTypeID { get; set; }

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
    [InverseProperty("BusinessEntityContact")]
    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    [ForeignKey("ContactTypeID")]
    [InverseProperty("BusinessEntityContact")]
    public virtual ContactType ContactType { get; set; } = null!;

    [ForeignKey("PersonID")]
    [InverseProperty("BusinessEntityContact")]
    public virtual Person Person { get; set; } = null!;
}
