using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Where to send a person email.
/// </summary>
[PrimaryKey("BusinessEntityID", "EmailAddressID")]
[Table("EmailAddress", Schema = "Person")]
[Index("EmailAddress1", Name = "IX_EmailAddress_EmailAddress")]
public partial class EmailAddress
{
    /// <summary>
    /// Primary key. Person associated with this email address.  Foreign key to Person.BusinessEntityID
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Primary key. ID of this email address.
    /// </summary>
    [Key]
    public int EmailAddressID { get; set; }

    /// <summary>
    /// E-mail address for the person.
    /// </summary>
    [Column("EmailAddress")]
    [StringLength(50)]
    public string? EmailAddress1 { get; set; }

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
    [InverseProperty("EmailAddress")]
    public virtual Person BusinessEntity { get; set; } = null!;
}
