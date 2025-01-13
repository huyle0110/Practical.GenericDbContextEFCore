using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vAdditionalContactInfo
{
    public int BusinessEntityID { get; set; }

    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string? MiddleName { get; set; }

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [StringLength(50)]
    public string? TelephoneNumber { get; set; }

    public string? TelephoneSpecialInstructions { get; set; }

    [StringLength(50)]
    public string? Street { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? StateProvince { get; set; }

    [StringLength(50)]
    public string? PostalCode { get; set; }

    [StringLength(50)]
    public string? CountryRegion { get; set; }

    public string? HomeAddressSpecialInstructions { get; set; }

    [StringLength(128)]
    public string? EMailAddress { get; set; }

    public string? EMailSpecialInstructions { get; set; }

    [StringLength(50)]
    public string? EMailTelephoneNumber { get; set; }

    public Guid rowguid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }
}
