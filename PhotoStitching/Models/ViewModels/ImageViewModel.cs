using System.ComponentModel.DataAnnotations;

namespace PhotoStitching.Models.ViewModels
{
    public class ImageViewModel
    {
        [DataType(DataType.Upload)]
        [Required(ErrorMessage = "Не задана картинка")]
        [Display(Name = "Загрузите картинку")]
        public IFormFile ImageFile { get; set; }

        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Не задана плотность")]
        [Display(Name = "Задайте плотность")]
        public double Density { get; set; }

        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Не задана плотность")]
        [Display(Name = "Задайте максимальный блок")]
        public int WidthMax { get; set; }
    }
}
