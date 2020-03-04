using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraEmguCV
{
    public sealed class TemplateImage
    {
        private Image<Bgr, byte> image;
        private Image<Bgr, byte> mask;

        static TemplateImage()
        {

        }

        private TemplateImage()
        {

        }

        public static TemplateImage Instance { get; } = new TemplateImage();

        public void SetTemplateImageAndMask(Image<Bgr,byte> image, Image<Bgr,byte> mask)
        {
            this.image = image;
            this.mask = mask;
        }

        public void SaveTemplate(string templateName)
        {
            CvInvoke.Imwrite("template_dir/" + templateName + ".jpg", this.image);
            CvInvoke.Imwrite("template_dir/" + templateName + "_mask.jpg", this.mask);
        }

        public Image<Bgr, byte> Image { get { return this.image; } }
        public Image<Bgr, byte> Mask { get { return this.mask; } }

    }
}
