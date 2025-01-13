using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Telephone number and type of a person.
/// </summary>
[PrimaryKey("BusinessEntityID", "PhoneNumber", "PhoneNumberTypeID")]
[Table("PersonPhone", Schema = "Person")]
[Index("PhoneNumber", Name = "IX_PersonPhone_PhoneNumber")]
public partial class PersonPhone
{
    /// <summary>
    /// Business entity identification number. Foreign key to Person.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Telephone number identification number.
    /// </summary>
    [Key]
    [StringLength(25)]
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// Kind of phone number. Foreign key to PhoneNumberType.PhoneNumberTypeID.
    /// </summary>
    [Key]
    public int PhoneNumberTypeID { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("BusinessEntityID")]
    [InverseProperty("PersonPhone")]
    public virtual Person BusinessEntity { get; set; } = null!;

    [ForeignKey("PhoneNumberTypeID")]
    [InverseProperty("PersonPhone")]
    public virtual PhoneNumberType PhoneNumberType { get; set; } = null!;
}
