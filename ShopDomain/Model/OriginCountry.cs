using ShopDomain.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopDomain.Model;

public partial class OriginCountry : Entity
{
    [StringLength(30)]
    [Display(Name = "Назва")]
    [Required(ErrorMessage = "Введіть країну.")]
    public string Name { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
