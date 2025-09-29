using ShopDomain.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopDomain.Model;

public partial class Category : Entity
{
    [Required(ErrorMessage = "Введіть назву категорії.")]
    [StringLength(20)]
    [Display(Name = "Категорія")]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
