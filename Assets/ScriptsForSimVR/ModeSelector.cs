using UnityEngine;

public class ModeSelector : MonoBehaviour
{
    public enum ModeOption { NormalModeSimulator, PlayMode , PreviewModeForTeleop , PreviewAndPlay, PreviewAR, Else }
    public ModeOption WhichMode;
    ROSClockPublisher Clock;

    void Start()
    {
        Clock = GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>();
    }
    void Update()
    {
        if (WhichMode == ModeOption.NormalModeSimulator) //simlator
        {
            //clock
            Clock.enabled = true;
        }
        if (WhichMode == ModeOption.PlayMode || WhichMode == ModeOption.PreviewAndPlay || WhichMode == ModeOption.PreviewAR)//visualization
        {
            //clock
            Clock.enabled = false;
        }

        if (WhichMode == ModeOption.PreviewModeForTeleop) //simlator+controller
        {
            //clock
            Clock.enabled = false;
        }

    }


}
