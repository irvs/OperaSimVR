using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using System.Collections.Generic;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.UrdfImporter;


public class Mongo_Joint_Writer : MonoBehaviour
{
    ROSConnection ros;
    public int sw;
    public string topicName = "/zx200_db_writer_joint_states";
    private JointStateMsg message;
    private List<ArticulationBody> joints;
    private List<string> jointNames;

    public bool enableJointEffortSensor = false;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.5f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    public string WriteTargetObject;
    GameObject targetobject;
    Controller_manager SW_From_cont;

    // Start is called before the first frame update
    void Start()
    {
        joints = new List<ArticulationBody>();
        jointNames = new List<string>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            if (joint.isActiveAndEnabled)
            {
                var ujoint = joint.GetComponent<UrdfJoint>();
                if (ujoint && !(ujoint is UrdfJointFixed))
                {
                    joints.Add(joint);
                    jointNames.Add(ujoint.jointName);
                }
            }
        }
        message = new JointStateMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();
        message.name = jointNames.ToArray();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<JointStateMsg>(topicName);
    }

    // Update is called once per constant rate
    void FixedUpdate()
    {
        SW_From_cont = FindObjectOfType<Controller_manager>();
        WriteTargetObject = SW_From_cont.Machine_name;
        if (sw == 1 || SW_From_cont.DB_joint_sw == true)
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed >= publishMessageInterval)
            {
                message.header.frame_id = "world";
                message.header.stamp = new TimeStamp(Clock.time);
                message.position = new double[joints.Count];
                message.velocity = new double[joints.Count];
                message.effort = new double[joints.Count];
                for (int i = 0; i < joints.Count; i++)
                {
                    message.position[i] = joints[i].jointPosition[0];
                    message.velocity[i] = joints[i].jointVelocity[0];
                    message.effort[i] = enableJointEffortSensor ? joints[i].driveForce[0] : 0.0;
                }
                ros.Publish(topicName, message);
                timeElapsed = 0.0f;
                Debug.Log(WriteTargetObject + " joint publish.");
                SW_From_cont.DB_joint_sw = false;
            }
        }
    }
}
