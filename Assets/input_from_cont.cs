using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class input_from_cont : MonoBehaviour
{
    void Update()
    {
       // Move();
       if (OVRInput.GetDown(OVRInput.RawButton.A))
    {
      //Aボタン
      Debug.Log("INPUT_AAAAAAAAAAAAAAAA");
    }
       if (OVRInput.GetDown(OVRInput.RawButton.X))
    {
      //Aボタン
      Debug.Log("INPUT_XXXXXXXXXXXXXXXX");
    }
    }

   /* void Move()
    {
        //右ジョイスティックの情報取得
        Vector2 stickR = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        Vector3 changePosition = new Vector3((stickR.x), 0, (stickR.y));
        //HMDのY軸の角度取得
        Vector3 changeRotation = new Vector3(0, InputTracking.GetLocalRotation(XRNode.Head).eulerAngles.y, 0);
        //OVRCameraRigの位置変更
        this.transform.position += this.transform.rotation * (Quaternion.Euler(changeRotation) * changePosition);
    }*/
}