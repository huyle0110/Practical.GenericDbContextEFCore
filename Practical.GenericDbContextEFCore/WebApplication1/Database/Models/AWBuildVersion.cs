using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Current version number of the AdventureWorks 2016 sample database. 
/// </summary>
public partial class AWBuildVersion
{
    /// <summary>
    /// Primary key for AWBuildVersion records.
    /// </summary>
    [Key]
    public byte SystemInformationID { get; set; }

    /// <summary>
    /// Version number of the database in 9.yy.mm.dd.00 format.
    /// </summary>
    [Column("Database Version")]
    [StringLength(25)]
    public string Database_Version { get; set; } = null!;

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime VersionDate { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }
}
