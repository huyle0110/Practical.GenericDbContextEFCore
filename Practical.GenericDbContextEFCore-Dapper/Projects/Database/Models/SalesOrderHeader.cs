using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// General sales order information.
/// </summary>
[Table("SalesOrderHeader", Schema = "Sales")]
[Index("SalesOrderNumber", Name = "AK_SalesOrderHeader_SalesOrderNumber", IsUnique = true)]
[Index("rowguid", Name = "AK_SalesOrderHeader_rowguid", IsUnique = true)]
[Index("CustomerID", Name = "IX_SalesOrderHeader_CustomerID")]
[Index("SalesPersonID", Name = "IX_SalesOrderHeader_SalesPersonID")]
public partial class SalesOrderHeader
{
    /// <summary>
    /// Primary key.
    /// </summary>
    [Key]
    public int SalesOrderID { get; set; }

    /// <summary>
    /// Incremental number to track changes to the sales order over time.
    /// </summary>
    public byte RevisionNumber { get; set; }

    /// <summary>
    /// Dates the sales order was created.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Date the order is due to the customer.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Date the order was shipped to the customer.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime? ShipDate { get; set; }

    /// <summary>
    /// Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled
    /// </summary>
    public byte Status { get; set; }

    /// <summary>
    /// 0 = Order placed by sales person. 1 = Order placed online by customer.
    /// </summary>
    public bool OnlineOrderFlag { get; set; }

    /// <summary>
    /// Unique sales order identification number.
    /// </summary>
    [StringLength(25)]
    public string SalesOrderNumber { get; set; } = null!;

    /// <summary>
    /// Customer purchase order number reference. 
    /// </summary>
    [StringLength(25)]
    public string? PurchaseOrderNumber { get; set; }

    /// <summary>
    /// Financial accounting number reference.
    /// </summary>
    [StringLength(15)]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Customer identification number. Foreign key to Customer.BusinessEntityID.
    /// </summary>
    public int CustomerID { get; set; }

    /// <summary>
    /// Sales person who created the sales order. Foreign key to SalesPerson.BusinessEntityID.
    /// </summary>
    public int? SalesPersonID { get; set; }

    /// <summary>
    /// Territory in which the sale was made. Foreign key to SalesTerritory.SalesTerritoryID.
    /// </summary>
    public int? TerritoryID { get; set; }

    /// <summary>
    /// Customer billing address. Foreign key to Address.AddressID.
    /// </summary>
    public int BillToAddressID { get; set; }

    /// <summary>
    /// Customer shipping address. Foreign key to Address.AddressID.
    /// </summary>
    public int ShipToAddressID { get; set; }

    /// <summary>
    /// Shipping method. Foreign key to ShipMethod.ShipMethodID.
    /// </summary>
    public int ShipMethodID { get; set; }

    /// <summary>
    /// Credit card identification number. Foreign key to CreditCard.CreditCardID.
    /// </summary>
    public int? CreditCardID { get; set; }

    /// <summary>
    /// Approval code provided by the credit card company.
    /// </summary>
    [StringLength(15)]
    [Unicode(false)]
    public string? CreditCardApprovalCode { get; set; }

    /// <summary>
    /// Currency exchange rate used. Foreign key to CurrencyRate.CurrencyRateID.
    /// </summary>
    public int? CurrencyRateID { get; set; }

    /// <summary>
    /// Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal TaxAmt { get; set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal Freight { get; set; }

    /// <summary>
    /// Total due from customer. Computed as Subtotal + TaxAmt + Freight.
    /// </summary>
    [Column(TypeName = "money")]
    public decimal TotalDue { get; set; }

    /// <summary>
    /// Sales representative comments.
    /// </summary>
    [StringLength(128)]
    public string? Comment { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("BillToAddressID")]
    [InverseProperty("SalesOrderHeaderBillToAddress")]
    public virtual Address BillToAddress { get; set; } = null!;

    [ForeignKey("CreditCardID")]
    [InverseProperty("SalesOrderHeader")]
    public virtual CreditCard? CreditCard { get; set; }

    [ForeignKey("CurrencyRateID")]
    [InverseProperty("SalesOrderHeader")]
    public virtual CurrencyRate? CurrencyRate { get; set; }

    [ForeignKey("CustomerID")]
    [InverseProperty("SalesOrderHeader")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("SalesOrder")]
    public virtual ICollection<SalesOrderDetail> SalesOrderDetail { get; set; } = new List<SalesOrderDetail>();

    [InverseProperty("SalesOrder")]
    public virtual ICollection<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReason { get; set; } = new List<SalesOrderHeaderSalesReason>();

    [ForeignKey("SalesPersonID")]
    [InverseProperty("SalesOrderHeader")]
    public virtual SalesPerson? SalesPerson { get; set; }

    [ForeignKey("ShipMethodID")]
    [InverseProperty("SalesOrderHeader")]
    public virtual ShipMethod ShipMethod { get; set; } = null!;

    [ForeignKey("ShipToAddressID")]
    [InverseProperty("SalesOrderHeaderShipToAddress")]
    public virtual Address ShipToAddress { get; set; } = null!;

    [ForeignKey("TerritoryID")]
    [InverseProperty("SalesOrderHeader")]
    public virtual SalesTerritory? Territory { get; set; }
}
