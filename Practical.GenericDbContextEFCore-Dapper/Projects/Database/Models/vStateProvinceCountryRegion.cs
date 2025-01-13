using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vStateProvinceCountryRegion
{
    public int StateProvinceID { get; set; }

    [StringLength(3)]
    public string StateProvinceCode { get; set; } = null!;

    public bool IsOnlyStateProvinceFlag { get; set; }

    [StringLength(50)]
    public string StateProvinceName { get; set; } = null!;

    public int TerritoryID { get; set; }

    [StringLength(3)]
    public string CountryRegionCode { get; set; } = null!;

    [StringLength(50)]
    public string CountryRegionName { get; set; } = null!;
}
