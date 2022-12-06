using Microsoft.AspNetCore.Mvc;
using PhotoStitching.Models.Classes;
using PhotoStitching.Models.ViewModels;
using PhotoStitching.Services;
using System.Drawing;

namespace PhotoStitching.Controllers
{
    public class ImageController : Controller
    {
        private readonly ImageConstructorService ics;

        public ImageController(ImageConstructorService ics)
        {
            this.ics = ics;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(ImageViewModel model)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Image was empty");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            UserClient.ImageFile = model.ImageFile;
            ics.Density = model.Density;
            ics.WidthMax = model.WidthMax;
            await ics.SetImage(model.ImageFile).WaitAsync(TimeSpan.FromMinutes(10));
            return RedirectToAction("Get", "Image");
        }

        [HttpGet]
        public IActionResult Get()
        {
            return View("Views/Image/Get.cshtml",ics.GetImage());
        }
    }
}
