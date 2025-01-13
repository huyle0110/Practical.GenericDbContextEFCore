using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vJobCandidateEducation
{
    public int JobCandidateID { get; set; }

    [Column("Edu.Level")]
    public string? Edu_Level { get; set; }

    [Column("Edu.StartDate", TypeName = "datetime")]
    public DateTime? Edu_StartDate { get; set; }

    [Column("Edu.EndDate", TypeName = "datetime")]
    public DateTime? Edu_EndDate { get; set; }

    [Column("Edu.Degree")]
    [StringLength(50)]
    public string? Edu_Degree { get; set; }

    [Column("Edu.Major")]
    [StringLength(50)]
    public string? Edu_Major { get; set; }

    [Column("Edu.Minor")]
    [StringLength(50)]
    public string? Edu_Minor { get; set; }

    [Column("Edu.GPA")]
    [StringLength(5)]
    public string? Edu_GPA { get; set; }

    [Column("Edu.GPAScale")]
    [StringLength(5)]
    public string? Edu_GPAScale { get; set; }

    [Column("Edu.School")]
    [StringLength(100)]
    public string? Edu_School { get; set; }

    [Column("Edu.Loc.CountryRegion")]
    [StringLength(100)]
    public string? Edu_Loc_CountryRegion { get; set; }

    [Column("Edu.Loc.State")]
    [StringLength(100)]
    public string? Edu_Loc_State { get; set; }

    [Column("Edu.Loc.City")]
    [StringLength(100)]
    public string? Edu_Loc_City { get; set; }
}
