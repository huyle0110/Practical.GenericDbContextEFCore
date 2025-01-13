using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vPersonDemographics
{
    public int BusinessEntityID { get; set; }

    [Column(TypeName = "money")]
    public decimal? TotalPurchaseYTD { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateFirstPurchase { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? BirthDate { get; set; }

    [StringLength(1)]
    public string? MaritalStatus { get; set; }

    [StringLength(30)]
    public string? YearlyIncome { get; set; }

    [StringLength(1)]
    public string? Gender { get; set; }

    public int? TotalChildren { get; set; }

    public int? NumberChildrenAtHome { get; set; }

    [StringLength(30)]
    public string? Education { get; set; }

    [StringLength(30)]
    public string? Occupation { get; set; }

    public bool? HomeOwnerFlag { get; set; }

    public int? NumberCarsOwned { get; set; }
}
