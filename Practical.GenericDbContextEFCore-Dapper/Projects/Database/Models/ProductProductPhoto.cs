using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Practical.GenericDbContextEFCore.Database.Models;

/// <summary>
/// Cross-reference table mapping products and product photos.
/// </summary>
[PrimaryKey("ProductID", "ProductPhotoID")]
[Table("ProductProductPhoto", Schema = "Production")]
public partial class ProductProductPhoto
{
    /// <summary>
    /// Product identification number. Foreign key to Product.ProductID.
    /// </summary>
    [Key]
    public int ProductID { get; set; }

    /// <summary>
    /// Product photo identification number. Foreign key to ProductPhoto.ProductPhotoID.
    /// </summary>
    [Key]
    public int ProductPhotoID { get; set; }

    /// <summary>
    /// 0 = Photo is not the principal image. 1 = Photo is the principal image.
    /// </summary>
    public bool Primary { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }

    [ForeignKey("ProductID")]
    [InverseProperty("ProductProductPhoto")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ProductPhotoID")]
    [InverseProperty("ProductProductPhoto")]
    public virtual ProductPhoto ProductPhoto { get; set; } = null!;
}
