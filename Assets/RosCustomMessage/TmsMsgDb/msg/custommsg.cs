using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace CustomMsgs
{
    public class MyCustomMessage : Message
    {
        public int data;
        public string label;

        public MyCustomMessage()
        {
            data = 0;
            label = "";
        }

        public MyCustomMessage(int data, string label)
        {
            this.data = data;
            this.label = label;
        }
    }
}
