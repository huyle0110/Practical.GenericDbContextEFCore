using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Customer credit card information.
/// </summary>
[Table("CreditCard", Schema = "Sales")]
[Index("CardNumber", Name = "AK_CreditCard_CardNumber", IsUnique = true)]
public partial class CreditCard
{
    /// <summary>
    /// Primary key for CreditCard records.
    /// </summary>
    [Key]
    public int CreditCardID { get; set; }

    /// <summary>
    /// Credit card name.
    /// </summary>
    [StringLength(50)]
    public string CardType { get; set; } = null!;

    /// <summary>
    /// Credit card number.
    /// </summary>
    [StringLength(25)]
    public string CardNumber { get; set; } = null!;

    /// <summary>
    /// Credit card expiration month.
    /// </summary>
    public byte ExpMonth { get; set; }

    /// <summary>
    /// Credit card expiration year.
    /// </summary>
    public short ExpYear { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("CreditCard")]
    public virtual ICollection<PersonCreditCard> PersonCreditCard { get; set; } = new List<PersonCreditCard>();

    [InverseProperty("CreditCard")]
    public virtual ICollection<SalesOrderHeader> SalesOrderHeader { get; set; } = new List<SalesOrderHeader>();
}
