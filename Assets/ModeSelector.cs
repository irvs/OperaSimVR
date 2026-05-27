using UnityEngine;

public class ModeSelector : MonoBehaviour
{
    public enum ModeOption { NormalModeSimulator, PlayMode , PreviewMode , Else }
    public ModeOption WhichMode;
    string WhichModePrev;
    private int prev_mode;
    private int mode_return;
    ROSClockPublisher Clock;

    void Start()
    {
        Clock = GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>();
    }
    void Update()
    {
        if (WhichMode.ToString() != WhichModePrev)
        {
            WhichModePrev= WhichMode.ToString();
        }

        if (WhichMode == ModeOption.NormalModeSimulator) //simlator
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = WhichMode.ToString();
            }      
            //clock
            Clock.enabled = true;
        }
        if (WhichMode == ModeOption.PlayMode)//visualization
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = WhichMode.ToString();
            }
            //clock
            Clock.enabled = false;
        }

        if (WhichMode == ModeOption.PreviewMode) //simlator+controller
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                if (WhichModePrev != "NormalModeSimulator")
                {
                 //   mode = 0;
                  //  mode_return = 2;
                }
                WhichModePrev = WhichMode.ToString();
            }
            //clock
            Clock.enabled = false;
        }

    }


}
