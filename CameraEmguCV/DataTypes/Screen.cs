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

        /*[DataMember]
        public String templatePath { get; set; }*/

        [DataMember(Order = 2)]
        public List<Point> coordinates { get; set; }

        [DataMember(Order = 3)]
        public List<Dial> dials { get; set; }

        [DataMember(Order = 4)]
        public byte[] TemplateImage { get; set; }

        [DataMember(Order = 7)]
        public byte[] TemplateMask { get; set; }
        
        [DataMember(Order = 5)]
        public int TemplateWidth { get; set; }

        [DataMember(Order = 6)]
        public int TemplateHeight { get; set; }

        public Emgu.CV.Image<Emgu.CV.Structure.Bgr,byte> GetTemplateImage()
        {   
            Emgu.CV.Image<Emgu.CV.Structure.Bgr,byte> image = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(TemplateWidth,TemplateHeight);
            if (TemplateImage != null)
                image.Bytes = TemplateImage;
            return image;
        }

        public Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> GetTemplateMask()
        {
            Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> image = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(TemplateWidth, TemplateHeight);
           if(TemplateMask!=null)
                image.Bytes = TemplateMask;
            return image;
        }

        public Screen(String name)
        {
            this.name = name;
            dials = new List<Dial>();
            coordinates = new List<Point>();
            
        }

    }
}
