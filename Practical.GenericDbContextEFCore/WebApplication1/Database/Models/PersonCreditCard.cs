using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Cross-reference table mapping people to their credit card information in the CreditCard table. 
/// </summary>
[PrimaryKey("BusinessEntityID", "CreditCardID")]
[Table("PersonCreditCard", Schema = "Sales")]
public partial class PersonCreditCard
{
    /// <summary>
    /// Business entity identification number. Foreign key to Person.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Credit card identification number. Foreign key to CreditCard.CreditCardID.
    /// </summary>
    [Key]
    public int CreditCardID { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("BusinessEntityID")]
    [InverseProperty("PersonCreditCard")]
    public virtual Person BusinessEntity { get; set; } = null!;

    [ForeignKey("CreditCardID")]
    [InverseProperty("PersonCreditCard")]
    public virtual CreditCard CreditCard { get; set; } = null!;
}
