using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.Core;


public class PoseStampedPublisher : MonoBehaviour
{
    ROSConnection ros;
    public string robotName = "robot_name";
    public string topicName = "robot_name/unity/pose_stmp";
    private PoseStampedMsg message;

    // Publish the object's position and rotation every N seconds
    public float publishMessageInterval = 0.05f;//20Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    public enum WorldMapOption { UnityLocal, RealWorld, RealMap }
    public WorldMapOption KindsOfMap;
    private float MapTransfer_x = 0;
    private float MapTransfer_y = 0;
    private float MapTransfer_z = 0;

    GameObject Reference;
    private Vector3 ReferencePointPose;
    private Quaternion ReferencePointRot;
    private Vector3 TargetPosition;
    private Quaternion TargetRotation;
    private Vector3 MapPosition;
    private Quaternion MapRotation;
    private Vector3 WorldPosition;
    public float MapRefetenceX;
    public float MapRefetenceY;
    public float MapRefetenceZ;

    // Start is called before the first frame update
    void Start()
    {
        message = new PoseStampedMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(topicName);
        Reference = GameObject.Find("MapReferencePoint");
        ReferencePointPose = Reference.transform.position;
        ReferencePointRot = Reference.transform.rotation;
    }

    // Update is called once per constant rate
    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;
        // Get Rigidbody
        ArticulationBody ab = this.transform.GetComponent<ArticulationBody> ();
    

        if (timeElapsed >= publishMessageInterval)
        {
            message.header.frame_id = "world";
            message.header.stamp = new TimeStamp(Clock.time);

            if (KindsOfMap.ToString() == "UnityLocal")
            {
                TargetPosition = this.transform.position;
                TargetRotation = this.transform.rotation;
            }
            else if (KindsOfMap.ToString() == "RealWorld")
            {
                MapPosition = this.transform.position - ReferencePointPose;
                WorldPosition = MapPosition + new Vector3(MapRefetenceY, MapRefetenceZ, MapRefetenceX);
                TargetPosition = new Vector3(WorldPosition.x, -WorldPosition.y, WorldPosition.z);
                TargetRotation = this.transform.rotation;
             //   Debug.Log("RealWorld");
            }
            else if (KindsOfMap.ToString() == "RealMap")
            {
                TargetPosition = this.transform.position - ReferencePointPose;
                TargetRotation = this.transform.rotation;
              //  Debug.Log("RealMap");
            }

            // Unity -> ROS transformation
            // Position: Unity(x,y,z) -> ROS(z,-x,y)
            // Quaternion: Unity(x,y,z,w) -> ROS(-z,x,-y,w)
            message.pose.position.x = TargetPosition.z;
            message.pose.position.y = -TargetPosition.x;
            message.pose.position.z = TargetPosition.y;

            message.pose.orientation.x = -TargetRotation.z;
            message.pose.orientation.y = TargetRotation.x;
            message.pose.orientation.z = -TargetRotation.y;
            message.pose.orientation.w = TargetRotation.w;

            ros.Publish(topicName, message);
            timeElapsed = 0.0f;
        }
    }
}
