using CustomMsgs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.TmsMsgDb;
using RosMessageTypes.Sensor;

public class test_publisher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class MyPublisher : MonoBehaviour
{

    ROSConnection ros;
    public string topicName = "joint_states";
    
    

    void Start()
    {
        ros.RegisterPublisher<MyCustomMessage>(topicName);

    }
    void Update()
    {
        MyCustomMessage message = new MyCustomMessage
        {
            label = "Test",
            data = 42
        };
        ros.Publish(topicName, message);
    }
}