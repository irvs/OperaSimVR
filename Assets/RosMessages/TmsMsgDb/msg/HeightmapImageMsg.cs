using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.TmsMsgDb
{
    public class HeightmapImageMsg : Message
    {
        public int data;
        public string label;

        public HeightmapImageMsg()
        {
            data = 0;
            label = "";
        }

        public HeightmapImageMsg(int data, string label)
        {
            this.data = data;
            this.label = label;
        }
    }
}
