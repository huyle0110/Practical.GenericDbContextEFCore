using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vEmployeeDepartment
{
    public int BusinessEntityID { get; set; }

    [StringLength(8)]
    public string? Title { get; set; }

    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string? MiddleName { get; set; }

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [StringLength(10)]
    public string? Suffix { get; set; }

    [StringLength(50)]
    public string JobTitle { get; set; } = null!;

    [StringLength(50)]
    public string Department { get; set; } = null!;

    [StringLength(50)]
    public string GroupName { get; set; } = null!;

    public DateOnly StartDate { get; set; }
}
