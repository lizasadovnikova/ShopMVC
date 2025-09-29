using ShopDomain.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopDomain.Model;

public partial class Item : Entity
{
    [Required(ErrorMessage = "Введіть назву товара.")]
    [Display(Name = "Назва")]
    [StringLength(20)]
    public string Name { get; set; } = null!;


    [Display(Name = "Опис")]
    [StringLength(50)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Оберіть країну походження.")]
    [Display(Name = "Країна походження")]
    public int CountryId { get; set; }

    [Required(ErrorMessage = "Оберіть категорію.")]
    [Display(Name = "Категорія")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Введіть ціну.")]
    [DisplayFormat(DataFormatString = "{0:0.0}", ApplyFormatInEditMode = true)]
    [Display(Name = "Ціна")]

    public decimal Price { get; set; }

    [Display(Name = "Фото")]
    public string? ImagePath { get; set; }

    [Display(Name = "Категорія")]
    public virtual Category Category { get; set; } = null!;

    [Display(Name = "Країна походження")]
    public virtual OriginCountry Country { get; set; } = null!;
}
