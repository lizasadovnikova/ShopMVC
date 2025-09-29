using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShopInfrastructure.ViewModels
{
    public class ItemEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва товару є обов'язковою.")]
        [Display(Name = "Назва")]
        [StringLength(20)]
        public string Name { get; set; } = null!;

        [Display(Name = "Опис")]
        [StringLength(50)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Країна походження є обов'язковою.")]
        [Display(Name = "Країна походження")]
        //[StringLength(20)]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "Категорія є обов'язковою.")]
        [Display(Name = "Категорія")]
        //[StringLength(20)]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Ціна є обов'язковою.")]
        [Range(0.5, double.MaxValue, ErrorMessage = "Ціна має бути більше 0.")]
        [DisplayFormat(DataFormatString = "{0:0.0}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ціна")]
        public decimal Price { get; set; }

        [Display(Name = "Фото")]
        public string? ImagePath { get; set; }

        [Display(Name = "Фото")]
        public IFormFile? ImageFile { get; set; }

        public string? CountryName { get; set; }
        public string? CategoryName { get; set; }
    }
}
