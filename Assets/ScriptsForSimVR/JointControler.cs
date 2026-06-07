using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter;

public class JointControler : MonoBehaviour
{
    ControllerManager VRManager;
    JointCommandPublisher FromVRJointController;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    public List<double> JointTargets;
    public enum JointContorollerModeOption { Velocity, Position }
    public JointContorollerModeOption JointContorollerMode;
    GameObject targetObject;
    ModeSelector mode;

    void Start()
    {
        targetObject = this.gameObject;
        FromVRJointController = GetComponent<JointCommandPublisher>();
        VRManager = FindObjectOfType<ControllerManager>();
        mode = FindObjectOfType<ModeSelector>();
    }
    void Update()
    {
        if ((mode.WhichMode == ModeSelector.ModeOption.NormalModeSimulator || mode.WhichMode == ModeSelector.ModeOption.PreviewModeForTeleop) && FromVRJointController.OnOffSw.ToString() == "On") 
        {
            if (VRManager.GetOnMachine == ControllerManager.RideOption.GetOn)
            {  
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
                                var drive = joint.xDrive;//targetjoints[i].xDrive;

                                if (JointContorollerMode == JointContorollerModeOption.Velocity)
                                {
                                    drive.driveType = ArticulationDriveType.Velocity;// = targetVelocity;
                                    drive.targetVelocity = (float)JointTargets[j];
                                    joint.xDrive = drive;
                                }
                                else if (JointContorollerMode == JointContorollerModeOption.Position)
                                {
                                    drive.driveType = ArticulationDriveType.Force;
                                    drive.target = (float)(JointTargets[j] * Mathf.Rad2Deg);
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

