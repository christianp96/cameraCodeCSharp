using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace CameraEmguCV
{
    [DataContract]
    class Dial
    {


        [DataMember]
        private String name;

        public String Name
        {
            get { return name; }
        }

        [DataMember]
        private String type;
        
        public String Type
        {
            get { return type; }
        }

        [DataMember]
        public  List<Point> coordinates { get; set; }

        public Dial(String name, String type)
        {
            this.name = name;
            this.type = type;
            coordinates = new List<Point>();
        }

        public Dial(String name, String type, List<Point> coordinates)
        {
            this.name = name;
            this.type = type;
            this.coordinates = new List<Point>();
            this.coordinates.AddRange(coordinates);
        }
    }
}
