using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vStoreWithAddresses
{
    public int BusinessEntityID { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string AddressType { get; set; } = null!;

    [StringLength(60)]
    public string AddressLine1 { get; set; } = null!;

    [StringLength(60)]
    public string? AddressLine2 { get; set; }

    [StringLength(30)]
    public string City { get; set; } = null!;

    [StringLength(50)]
    public string StateProvinceName { get; set; } = null!;

    [StringLength(15)]
    public string PostalCode { get; set; } = null!;

    [StringLength(50)]
    public string CountryRegionName { get; set; } = null!;
}
