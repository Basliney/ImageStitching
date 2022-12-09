using PhotoStitching.Models.Classes;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Numerics;

namespace PhotoStitching.Services
{
    // y0_AgAAAAAUCAmRAADLWwAAAADUkpcZMPZpT7eQR66oat0xwmxwzNartu0

    public class ImageConstructorService
    {
        private Bitmap middleLayer;
        private List<int> exceptionList = new List<int>();
        private Random rnd = new Random();

        private Image image { get; set; }
        private Image OutputImage { get; set; }
        public double Density { get; set; }
        public int WidthMax { get; set; }

        private List<FileImage> dataImages = new List<FileImage>();
        private List<string> files = new List<string>();

        async public Task SetImage(IFormFile image)
        {
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                await image.CopyToAsync(memoryStream);  // Упаковываем файл в поток
                var img = Image.FromStream(memoryStream);   // Вытаскиваем из потока как изображение
                middleLayer = new Bitmap(img,new Size(1024, 1024));  // Передаем в промежуточный слой
                ImageConstructor(); // Вызов контруктора изображений
                dataImages.Clear();
                GC.Collect();
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Image can't be sent to stream or retrieved from stream: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                memoryStream.Close();   // В любом случае закрываем поток
            }
        }

        /// <summary>
        /// Конструктор изображения
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]
        public async Task ImageConstructor()
        {
            var res = middleLayer;
            using (var graphic = Graphics.FromImage(res))
            {
                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = CompositingQuality.HighQuality;
            }
            middleLayer = res;

            OutputImage = await CutTheImage(middleLayer);
        }

        private async Task<Bitmap> CutTheImage(Bitmap bitmap)
        {
            var width = bitmap.Width; var height = bitmap.Height;
            int countOfPair = 6;
            double lengthBetweenColor = Density + 2;
            
            Bitmap firstBitmap = new Bitmap(bitmap.Width / 2, bitmap.Height / 2);
            for (int i = 0; i < firstBitmap.Width; i++)
            {
                for (int j = 0; j < firstBitmap.Height; j++)
                {
                    firstBitmap.SetPixel(i, j, bitmap.GetPixel(i, j));
                    //Debug.WriteLine($"j = {j}");
                }
                //Debug.WriteLine($"i first = {i}");
            }

            Bitmap lastCuttedBitmap = new Bitmap(bitmap.Width / 2, bitmap.Height / 2);
            for(int i = 0;i < lastCuttedBitmap.Width; i++)
            {
                for (int j = 0; j < lastCuttedBitmap.Height; j++)
                {
                    lastCuttedBitmap.SetPixel(i, j, bitmap.GetPixel(i + lastCuttedBitmap.Width, j + lastCuttedBitmap.Height));
                    //Debug.WriteLine($"j = {j}");
                }
                //Debug.WriteLine($"i first = {i}");
            }

            Bitmap prelastCuttedBitmap = new Bitmap(lastCuttedBitmap);
            for (int i = 0; i < prelastCuttedBitmap.Width; i++)
            {
                for (int j = 0; j < prelastCuttedBitmap.Height; j++)
                {
                    prelastCuttedBitmap.SetPixel(i, j, bitmap.GetPixel(i, j + prelastCuttedBitmap.Height));
                    //Debug.WriteLine($"j sec = {j}");
                }
                //Debug.WriteLine($"i sec = {i}");
            }

            Bitmap secondCuttedBitmap = new Bitmap(lastCuttedBitmap);
            for (int i = 0; i < secondCuttedBitmap.Width; i++)
            {
                for (int j = 0; j < secondCuttedBitmap.Height; j++)
                {
                    secondCuttedBitmap.SetPixel(i, j, bitmap.GetPixel(i + secondCuttedBitmap.Width, j ));
                    //Debug.WriteLine($"j sec = {j}");
                }
                //Debug.WriteLine($"i third = {i}");
            }

            Color firstColor = MeanColor(firstBitmap);
            Color secondColor = MeanColor(secondCuttedBitmap);
            Color thirdColor = MeanColor(prelastCuttedBitmap);
            Color fourthColor = MeanColor(lastCuttedBitmap);


            var labFC = RGBToLab(firstColor);//XYZtoLAB(RGBtoXYZ(firstColor));
            var labSC = RGBToLab(secondColor);//XYZtoLAB(RGBtoXYZ(secondColor));
            var labTC = RGBToLab(thirdColor);//XYZtoLAB(RGBtoXYZ(thirdColor));
            var labLC = RGBToLab(fourthColor);//XYZtoLAB(RGBtoXYZ(fourthColor));    // Last

            var dist1 = GetDistance(labFC, labSC);
            var dist2 = GetDistance(labSC, labTC);
            var dist3 = GetDistance(labTC, labLC);
            var dist4 = GetDistance(labLC, labFC);
            var dist5 = GetDistance(labFC, labTC);
            var dist6 = GetDistance(labSC, labLC);
            //Debug.WriteLine($"i m here");
            var meanDist = (dist1 + dist2 + dist3 + dist4 + dist5 + dist6) / countOfPair;
            if (meanDist >= lengthBetweenColor && width > WidthMax)
            {
                firstBitmap = await CutTheImage(firstBitmap);
                secondCuttedBitmap = await CutTheImage(secondCuttedBitmap);
                prelastCuttedBitmap = await CutTheImage(prelastCuttedBitmap);
                lastCuttedBitmap = await CutTheImage(lastCuttedBitmap);
            }
            else
            {
                var firstSimilar = await GetSimilarPhoto(labFC, firstBitmap);
                var secondSimilar = await GetSimilarPhoto(labSC, secondCuttedBitmap);
                var thirdSimilar = await GetSimilarPhoto(labTC, prelastCuttedBitmap);
                var fourthSimilar = await GetSimilarPhoto(labLC, lastCuttedBitmap);

                for (int i = 0; i < width / 2; i++)
                {
                    for (int j = 0; j < height / 2; j++)
                    {

                        firstBitmap.SetPixel(i, j, firstSimilar.GetPixel(i, j));//firstColor);
                        secondCuttedBitmap.SetPixel(i, j, secondSimilar.GetPixel(i, j)); //*/secondColor);
                        prelastCuttedBitmap.SetPixel(i, j, thirdSimilar.GetPixel(i, j)); //*/thirdColor);
                        lastCuttedBitmap.SetPixel(i, j, fourthSimilar.GetPixel(i, j));// */fourthColor);
                    }
                }
            }

            for(int i = 0; i < width / 2; i++)
            {
                for(int j = 0; j < height / 2; j++)
                {
                    bitmap.SetPixel(i, j, firstBitmap.GetPixel(i, j));
                    bitmap.SetPixel(i + width / 2, j, secondCuttedBitmap.GetPixel(i, j));
                    bitmap.SetPixel(i, j + height / 2, prelastCuttedBitmap.GetPixel(i, j));
                    bitmap.SetPixel(i + width / 2, j + height / 2, lastCuttedBitmap.GetPixel(i, j));

                    //if (i == 0 || i == width / 2 - 1 || j == 0 || j == height / 2 - 1)
                    //{
                    //    bitmap.SetPixel(i, j, Color.White);
                    //    bitmap.SetPixel(i + width / 2, j, Color.White);
                    //    bitmap.SetPixel(i, j + height / 2, Color.White);
                    //    bitmap.SetPixel(i + width / 2, j + height / 2, Color.White);
                    //}
                }
            }
            return bitmap;
        }

        private async Task<Bitmap> GetSimilarPhoto((double, double, double) labColor, Bitmap currentBitmap)
        {

            if (files.Count == 0)
            {
                files.AddRange(Directory.GetFiles($"D:\\Downloads\\dataBase\\VG_100K_2"));
                PhotoAnalizer(files);
            }

            Bitmap img = new Bitmap(currentBitmap.Width, currentBitmap.Height);
            var anyImage = GetMinDistance(labColor, out var minImage);    //dataImages.Min(x => GetDistance(x.LABColor, labColor) < Density + 2);
            if (anyImage.Count() > 0)
            {
                var selectedObject = anyImage.ElementAt(rnd.Next(anyImage.Count()));
                img = new Bitmap(Image.FromFile(selectedObject.Path), new Size(currentBitmap.Width, currentBitmap.Height));

                img = ColorTransform(img, currentBitmap);
            }
            else
            {
                img = new Bitmap(Image.FromFile(minImage.Path), new Size(currentBitmap.Width, currentBitmap.Height));
                img = ColorTransform(img, currentBitmap);
            }
            return img;
        }

        private List<FileImage> GetMinDistance((double, double, double) labColor, out FileImage minImage)
        {
            List<FileImage> result = new List<FileImage>();
            minImage = dataImages.First();
            var distance = GetDistance(minImage.LABColor, labColor);

            foreach(var item in dataImages)
            {
                var newDistance = GetDistance(item.LABColor, labColor);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    minImage = item;
                }
                if (newDistance < Density + 10)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private Bitmap ColorTransform(Bitmap img, Bitmap currentBitmap)
        {

            Bitmap transformedBitmap = new Bitmap(img);

            for (int i =0;i< transformedBitmap.Width; i++)
            {
                for (int j = 0; j < transformedBitmap.Height; j++)
                {
                    Color meanColor = GetMeanColorOfPixels(img.GetPixel(i, j), currentBitmap.GetPixel(i, j));
                    transformedBitmap.SetPixel(i, j, meanColor);
                }
            }

            return transformedBitmap;
        }

        private Color GetMeanColorOfPixels(Color newColor, Color targetColor)
        {
            return Color.FromArgb((newColor.R + targetColor.R) / 2, (newColor.G + targetColor.G) / 2, (newColor.B + targetColor.B) / 2);
        }

        private void PhotoAnalizer(List<string> files)
        {
            for (int i = dataImages.Count; i < Math.Min(files.Count, 700); i++)
            {
                try
                {
                    var imgSmall = new Bitmap(Image.FromFile(files[i]), new Size(8, 8));
                    var meanColor = MeanColor(imgSmall);
                    var meanLABColor = RGBToLab(meanColor);
                    dataImages.Add(new FileImage(files[i], meanColor, meanLABColor));
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }

        private double GetDistance((double,double,double) lab1, (double, double, double) lab2)
        {
            var deltaL = lab1.Item1 - lab2.Item1;
            var deltaA = lab1.Item2 - lab2.Item2;
            var deltaB = lab1.Item3 - lab2.Item3;

            var c1 = Math.Sqrt(lab1.Item2 * lab1.Item2 + lab1.Item3 * lab1.Item3);
            var c2 = Math.Sqrt(lab2.Item2 * lab2.Item2 + lab2.Item3 * lab2.Item3);
            //var c2 = Math.Sqrt(Math.Pow(lab2.Item2, 2) + Math.Pow(lab2.Item3, 2));
            var finalResult = Math.Sqrt(deltaL*deltaL + deltaA*deltaA + deltaB * deltaB);

            return finalResult;
        }

        private Color MeanColor(Bitmap bitmap)
        {
            int r = 0, g = 0, b = 0;
            int global_r = 0, global_g = 0, global_b = 0;
            int c = 1, j = 1;
            for (c = 0; c < bitmap.Width / 2; c++)
            {
                for (j = 0; j < bitmap.Height / 2; j++)
                {
                    r += bitmap.GetPixel(c, j).R;
                    g += bitmap.GetPixel(c, j).G;
                    b += bitmap.GetPixel(c, j).B;
                }
                global_r += r; r = 0;
                global_g += g; g = 0;
                global_b += b; b = 0;
            }
            global_r /= (Math.Max(c, 1) * Math.Max(j, 1));
            global_g /= (Math.Max(c, 1) * Math.Max(j, 1));
            global_b /= (Math.Max(c, 1) * Math.Max(j, 1));

            return Color.FromArgb(global_r, global_g, global_b);
        }

        private (float,float,float) RGBToLab(Color color)
        {
            float[] xyz = new float[3];
            float[] lab = new float[3];
            float[] rgb = new float[] { color.R, color.G, color.B };

            rgb[0] = color.R / 255.0f;
            rgb[1] = color.G / 255.0f;
            rgb[2] = color.B / 255.0f;

            if (rgb[0] > .04045f)
            {
                rgb[0] = (float)Math.Pow((rgb[0] + .055) / 1.055, 2.4);
            }
            else
            {
                rgb[0] = rgb[0] / 12.92f;
            }

            if (rgb[1] > .04045f)
            {
                rgb[1] = (float)Math.Pow((rgb[1] + .055) / 1.055, 2.4);
            }
            else
            {
                rgb[1] = rgb[1] / 12.92f;
            }

            if (rgb[2] > .04045f)
            {
                rgb[2] = (float)Math.Pow((rgb[2] + .055) / 1.055, 2.4);
            }
            else
            {
                rgb[2] = rgb[2] / 12.92f;
            }
            rgb[0] = rgb[0] * 100.0f;
            rgb[1] = rgb[1] * 100.0f;
            rgb[2] = rgb[2] * 100.0f;


            xyz[0] = ((rgb[0] * .412453f) + (rgb[1] * .357580f) + (rgb[2] * .180423f));
            xyz[1] = ((rgb[0] * .212671f) + (rgb[1] * .715160f) + (rgb[2] * .072169f));
            xyz[2] = ((rgb[0] * .019334f) + (rgb[1] * .119193f) + (rgb[2] * .950227f));


            xyz[0] = xyz[0] / 95.047f;
            xyz[1] = xyz[1] / 100.0f;
            xyz[2] = xyz[2] / 108.883f;

            if (xyz[0] > .008856f)
            {
                xyz[0] = (float)Math.Pow(xyz[0], (1.0 / 3.0));
            }
            else
            {
                xyz[0] = (xyz[0] * 7.787f) + (16.0f / 116.0f);
            }

            if (xyz[1] > .008856f)
            {
                xyz[1] = (float)Math.Pow(xyz[1], 1.0 / 3.0);
            }
            else
            {
                xyz[1] = (xyz[1] * 7.787f) + (16.0f / 116.0f);
            }

            if (xyz[2] > .008856f)
            {
                xyz[2] = (float)Math.Pow(xyz[2], 1.0 / 3.0);
            }
            else
            {
                xyz[2] = (xyz[2] * 7.787f) + (16.0f / 116.0f);
            }

            lab[0] = (116.0f * xyz[1]) - 16.0f;
            lab[1] = 500.0f * (xyz[0] - xyz[1]);
            lab[2] = 200.0f * (xyz[1] - xyz[2]);

            return (lab[0], lab[1], lab[2]);
        }

        /// <summary>
        /// Метод извеления изображения
        /// </summary>
        /// <returns>Ссылка на изображение</returns>
        public string GetImage()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (OutputImage == null)
                {
                    return "";
                }
                OutputImage.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);   // Сохраняем выходное изображение в потоке
                ms.Position = 0;    // Указатель в потоке ставим на начало
                return Convert.ToBase64String(ms.ToArray());    // Конвертируем поток в base64
            }
        }
    }
}
