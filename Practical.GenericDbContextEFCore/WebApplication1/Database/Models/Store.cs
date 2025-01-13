using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Customers (resellers) of Adventure Works products.
/// </summary>
[Table("Store", Schema = "Sales")]
[Index("rowguid", Name = "AK_Store_rowguid", IsUnique = true)]
[Index("SalesPersonID", Name = "IX_Store_SalesPersonID")]
[Index("Demographics", Name = "PXML_Store_Demographics")]
public partial class Store
{
    /// <summary>
    /// Primary key. Foreign key to Customer.BusinessEntityID.
    /// </summary>
    [Key]
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Name of the store.
    /// </summary>
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// ID of the sales person assigned to the customer. Foreign key to SalesPerson.BusinessEntityID.
    /// </summary>
    public int? SalesPersonID { get; set; }

    /// <summary>
    /// Demographic informationg about the store such as the number of employees, annual sales and store type.
    /// </summary>
    [Column(TypeName = "xml")]
    public string? Demographics { get; set; }

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
    [InverseProperty("Store")]
    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    [InverseProperty("Store")]
    public virtual ICollection<Customer> Customer { get; set; } = new List<Customer>();

    [ForeignKey("SalesPersonID")]
    [InverseProperty("Store")]
    public virtual SalesPerson? SalesPerson { get; set; }
}
