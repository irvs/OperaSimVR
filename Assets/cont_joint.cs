using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using System;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Drawing;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.Core;
using RosMessageTypes.Nav;

public class cont_joint : MonoBehaviour
{
    Controller_manager VRManager;
    mode_selector selected_mode;
    FieldMainManager PoseVeloSelector;
    JointAnglePublisher FromVRJointController;
    public int sw = 0;
    ROSConnection ros;
    public float publishMessageFrequency = 0.5f;
    private float timeElapsed;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    private double targetPos;
    public enum JointContorollerModeOption { Velocity, Position }
    public JointContorollerModeOption JointContorollerMode;

    GameObject targetPlayerObject;
    GameObject targetObject;

    void Start()
    {
        targetPlayerObject = GameObject.Find("OVRPlayerController");
        targetObject = this.gameObject;
    }
    void Update()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        PoseVeloSelector = FindObjectOfType<FieldMainManager>();
        if (sw == 1)
        {
            if (VRManager.PlayerPoseMove_SW > 0 || sw == 1)
            {
                selected_mode = FindObjectOfType<mode_selector>();
                OVRPlayerController scriptA = targetPlayerObject.GetComponent<OVRPlayerController>();
                if (scriptA != null)
                {
                    scriptA.RotationRatchet = 0;
                    scriptA.RotationAmount = 0;
                }

                if(PoseVeloSelector.ForSimOrReal.ToString() == "ForSimPhysX")
                {
                    JointContorollerMode = JointContorollerModeOption.Position;
                }
                else if (PoseVeloSelector.ForSimOrReal.ToString() == "ForSimAGX")
                {
                    JointContorollerMode = JointContorollerModeOption.Velocity;
                }
                else if (PoseVeloSelector.ForSimOrReal.ToString() == "ForReal")
                {
                    JointContorollerMode = JointContorollerModeOption.Velocity;
                }

                timeElapsed += Time.deltaTime;
                if (timeElapsed > publishMessageFrequency)
                {
                    timeElapsed = 0;
                }
                
                FromVRJointController = GetComponent<JointAnglePublisher>();

                targetjoints = new List<ArticulationBody>();
                targetjointNames = new List<string>();
                //
                int j = 0;
                foreach (var joint in targetObject.GetComponentsInChildren<ArticulationBody>())
                {
                    if (joint.isActiveAndEnabled)
                    {
                        var targetujoint = joint.GetComponent<UrdfJoint>();
                        if (targetujoint && !(targetujoint is UrdfJointFixed))
                        {
                            targetjoints.Add(joint);
                            targetjointNames.Add(targetujoint.jointName);
                            //
                            if (FromVRJointController.listOfJointCmdList.Count - 1 >= 0)
                            {
                                targetPos = FromVRJointController.listOfJointCmdList[FromVRJointController.listOfJointCmdList.Count - 1][j];
                               // Debug.Log(targetPos);
                                if (j == 2)
                                {
                                  //  targetPos = targetPos * 0.05;
                                }
                               // targetPos = targetPos * 500.0f;
                                var drive = joint.xDrive;//targetjoints[i].xDrive;

                                if (JointContorollerMode.ToString() == "Velocity")
                                {
                                    drive.driveType = ArticulationDriveType.Velocity;// = targetVelocity;
                                    drive.targetVelocity = (float)targetPos;
                                    joint.xDrive = drive;
                                }
                                else if (JointContorollerMode.ToString() == "Position")
                                {
                                    drive.driveType = ArticulationDriveType.Force;
                                    drive.target = (float)(targetPos * Mathf.Rad2Deg);
                                    joint.xDrive = drive;//targetjoints[i].xDrive = drive;
                                }
                                j += 1;
                            }
                        }
                    }
                }
            }
        }
    }
}

