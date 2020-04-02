using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;


namespace CameraEmguCV
{
    [DataContract]
    class Screen
    {
        [DataMember(Order = 1)]
        private String name { get; set; }

        [DataMember(Order = 2)] 
        public String templatePath { get; set; }

        [DataMember(Order = 3)]
        public List<Point> coordinates { get; set; }

        [DataMember(Order = 4)]
        public List<Dial> dials { get; set; }

        public Emgu.CV.Mat TemplateImage { get; set; }

        public Emgu.CV.Mat TemplateMask { get; set; }
       
        public Screen(String name)
        {
            this.name = name;
            dials = new List<Dial>();
            coordinates = new List<Point>();
        }

        public void SaveTemplate()
        {
            Emgu.CV.CvInvoke.Imwrite(templatePath + ".jpg", this.TemplateImage);
            Emgu.CV.CvInvoke.Imwrite(templatePath + "_mask.jpg", this.TemplateMask);
        }

        public void LoadTemplate()
        {
            TemplateImage = Emgu.CV.CvInvoke.Imread(templatePath + ".jpg", Emgu.CV.CvEnum.LoadImageType.Color);
            TemplateMask = Emgu.CV.CvInvoke.Imread(templatePath + "_mask.jpg", Emgu.CV.CvEnum.LoadImageType.Color);
        }
    }
}
