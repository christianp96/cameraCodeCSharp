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
        private String name { get; set; }

        [DataMember]
        private String type { get; set; }

        [DataMember]
        public  List<Point> coordinates { get; set; }

        public Dial(String name, String type)
        {
            this.name = name;
            this.type = type;
            coordinates = new List<Point>();
        }
    }
}
