using System.Drawing;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Sensor;
using System;

namespace RosMessageTypes.TmsMsgDb
{
    [Serializable]

    public class HeightmapImageRequest : Message
    {
        public int data;
        public string label;

        /*
        public HeightmapImageRequest()
        {
            data = 0;
            label = "";
        }
        */

    }

    public class HeightmapImageResponse : Message
    {
        public int data;
        public string label;
        public ImageMsg HeightmapImage;

        /*
        public HeightmapImageResponse(int data, string label)
        {
            this.data = data;
            this.label = label;
        }
        */
    }

}
