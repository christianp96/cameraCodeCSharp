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
        private String expectedValue;
        
        public String ExpectedValue
        {
            get { return expectedValue; }
        }


        //private String identifiedValue;

        //public String IdentifiedValue
        //{
        //    set { identifiedValue = value; }
        //}
        [DataMember]
        public  List<Point> coordinates { get; set; }

        public Dial(String name, String type, String expectedValue)
        {
            this.name = name;
            this.type = type;
            this.expectedValue = expectedValue;
            coordinates = new List<Point>();
        }

        public Dial(String name, String type,String expectedValue, List<Point> coordinates)
        {
            this.name = name;
            this.type = type;
            this.expectedValue = expectedValue;
            this.coordinates = new List<Point>();
            this.coordinates.AddRange(coordinates);
        }
    }
}
