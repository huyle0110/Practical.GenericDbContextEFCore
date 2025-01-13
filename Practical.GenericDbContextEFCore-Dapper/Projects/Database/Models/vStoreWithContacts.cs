using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vStoreWithContacts
{
    public int BusinessEntityID { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string ContactType { get; set; } = null!;

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

    [StringLength(25)]
    public string? PhoneNumber { get; set; }

    [StringLength(50)]
    public string? PhoneNumberType { get; set; }

    [StringLength(50)]
    public string? EmailAddress { get; set; }

    public int EmailPromotion { get; set; }
}
