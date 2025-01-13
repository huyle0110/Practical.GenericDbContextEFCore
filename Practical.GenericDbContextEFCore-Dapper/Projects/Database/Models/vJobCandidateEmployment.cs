using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

[Keyless]
public partial class vJobCandidateEmployment
{
    public int JobCandidateID { get; set; }

    [Column("Emp.StartDate", TypeName = "datetime")]
    public DateTime? Emp_StartDate { get; set; }

    [Column("Emp.EndDate", TypeName = "datetime")]
    public DateTime? Emp_EndDate { get; set; }

    [Column("Emp.OrgName")]
    [StringLength(100)]
    public string? Emp_OrgName { get; set; }

    [Column("Emp.JobTitle")]
    [StringLength(100)]
    public string? Emp_JobTitle { get; set; }

    [Column("Emp.Responsibility")]
    public string? Emp_Responsibility { get; set; }

    [Column("Emp.FunctionCategory")]
    public string? Emp_FunctionCategory { get; set; }

    [Column("Emp.IndustryCategory")]
    public string? Emp_IndustryCategory { get; set; }

    [Column("Emp.Loc.CountryRegion")]
    public string? Emp_Loc_CountryRegion { get; set; }

    [Column("Emp.Loc.State")]
    public string? Emp_Loc_State { get; set; }

    [Column("Emp.Loc.City")]
    public string? Emp_Loc_City { get; set; }
}
