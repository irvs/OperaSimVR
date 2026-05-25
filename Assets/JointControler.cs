using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter;




public class JointControler : MonoBehaviour
{
    Controller_manager VRManager;
    JointAnglePublisher FromVRJointController;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    public List<double> JointTargets;
    double targetPos;
    public enum JointContorollerModeOption { Velocity, Position }
    public JointContorollerModeOption JointContorollerMode;

    GameObject targetPlayerObject;
    GameObject targetObject;

    mode_selector mode;

    void Start()
    {
        targetPlayerObject = GameObject.Find("OVRPlayerController");
        targetObject = this.gameObject;
        VRManager = FindObjectOfType<Controller_manager>();
        mode = FindObjectOfType<mode_selector>();
    }
    void Update()
    {
        FromVRJointController = GetComponent<JointAnglePublisher>();

        if ((mode.mode == 0 || mode.mode == 2) && FromVRJointController.OnOffSw.ToString() == "On") 
        {
            if (VRManager.PlayerPoseMove_SW > 0)
            {
                OVRPlayerController scriptA = targetPlayerObject.GetComponent<OVRPlayerController>();
                if (scriptA != null)
                {
                    scriptA.RotationRatchet = 0;
                    scriptA.RotationAmount = 0;
                }
              
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
                            if (FromVRJointController.listOfJointVelocityCmdList.Count - 1 >= 0)
                            {
                                targetPos = FromVRJointController.listOfJointVelocityCmdList[FromVRJointController.listOfJointVelocityCmdList.Count - 1][j];
                                var drive = joint.xDrive;//targetjoints[i].xDrive;

                                if (JointContorollerMode == JointContorollerModeOption.Velocity)
                                {
                                    drive.driveType = ArticulationDriveType.Velocity;// = targetVelocity;
                                    drive.targetVelocity = (float)targetPos;
                                    joint.xDrive = drive;
                                }
                                else if (JointContorollerMode == JointContorollerModeOption.Position)
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

