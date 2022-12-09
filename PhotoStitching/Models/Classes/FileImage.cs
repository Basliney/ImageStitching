using System.Drawing;

namespace PhotoStitching.Models.Classes
{
    public class FileImage
    {
        public string Path { get; }
        public Color MeanColor { get; }
        public (double, double, double) LABColor { get; }

        public FileImage(string path, Color meanColor, (double, double, double) lABColor)
        {
            this.Path = path;
            this.MeanColor = meanColor;
            this.LABColor = lABColor;
        }
    }
}
