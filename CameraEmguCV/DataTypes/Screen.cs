using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;


namespace CameraEmguCV
{
    [DataContract]
    class Screen
    {
        [DataMember]
        private String name { get; set; }

        [DataMember]
        private String templatePath { get; set; }

        [DataMember]
        public List<Point> coordinates { get; set; }

        [DataMember]
        public List<Dial> dials { get; set; }

        public Screen(String name)
        {
            this.name = name;
            dials = new List<Dial>();
            coordinates = new List<Point>();
        }

    }
}
