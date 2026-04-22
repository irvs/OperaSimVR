using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
//using System.Drawing.Printing;
using System.Linq;

using static UnityEngine.GraphicsBuffer;
using static UnityEditor.PlayerSettings;
using RosMessageTypes.Std;
using UnityEngine.WSA;

//using RosSharp.RosBridgeClient;
//using std_msgs = Ros.RosBridgeClient.MessageTypes.Standard;

public class ControllerSW : MonoBehaviour
{
    private float timeElapsed=0.0f;
    public bool controller_sw;
    private BoolMsg Published_controller_sw;
    public string PublishTopicName;
    ROSConnection ros;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(PublishTopicName);
    }

    // Update is called once per frame
    void Update()
    {
        if (controller_sw == true)
        {
            Published_controller_sw.data = true;
        }
        else if (controller_sw == false)
        {
            Published_controller_sw.data = false;
        }
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 0.02f)
        {
            // Debug.Log("Publish After Delay Time");
            ros.Publish(PublishTopicName, Published_controller_sw);
            timeElapsed = 0.0f;
        }
    }
}
