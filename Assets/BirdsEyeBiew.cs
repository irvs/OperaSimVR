using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
public class JoyStickMove : MonoBehaviour
{
    int birdseyeview = 0;
    public float updownspeed = 5.0f;
    public float movespeed = 0.10f;
    public float rotspeed = 10.0f;
    Vector3 posiorigin;
    Quaternion rotrigin;
    /*
    void Start()
    {
        Vector3 posiplayer = GameObject.Find("OVRPlayerController").transform.position;
        Quaternion rotplayer = GameObject.Find("OVRPlayerController").transform.rotation;
    }*/
    void Update()
    {
        Vector3 posiplayer = GameObject.Find("OVRPlayerController").transform.position;
        Quaternion rotplayer = GameObject.Find("OVRPlayerController").transform.rotation;
        GameObject PlayerObject = GameObject.Find("OVRPlayerController");
        if (OVRInput.Get(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.Y) && birdseyeview == 0)
        {
            //BirdsEyeView();
            Debug.Log("bird's eye view");
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
            birdseyeview = 1;
            //Vector3 posiplayer = GameObject.Find("OVRPlayerController").transform.position;
            //Quaternion rotplayer  = GameObject.Find("OVRPlayerController").transform.rotation;
            posiorigin = GameObject.Find("OVRPlayerController").transform.position;
            rotrigin = GameObject.Find("OVRPlayerController").transform.rotation;
        }
        //up
        if (OVRInput.Get(OVRInput.RawButton.X) && birdseyeview == 1)
        {
            //posiplayer += new Vector3(0, updownspeed, 0);
            GameObject.Find("OVRPlayerController").transform.position = GameObject.Find("OVRPlayerController").transform.position+new Vector3(0, updownspeed, 0);
            Debug.Log("up");
            Debug.Log(GameObject.Find("OVRPlayerController").transform.position);
        }
        //down
        if (OVRInput.Get(OVRInput.RawButton.Y) && birdseyeview == 1)
        {
            GameObject.Find("OVRPlayerController").transform.position = GameObject.Find("OVRPlayerController").transform.position + new Vector3(0, -updownspeed, 0);
            Debug.Log("Down");
            Debug.Log(GameObject.Find("OVRPlayerController").transform.position);
        }
        //position
        if (birdseyeview == 1)
        {
            //左ジョイスティックの情報取得
            Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
            //OVRCameraRigの位置変更
            GameObject.Find("OVRPlayerController").transform.position += GameObject.Find("OVRPlayerController").transform.rotation * (new Vector3((stickL.x), 0, (stickL.y)));
        }
        //rotation
        if (birdseyeview == 1)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft))
            {
                PlayerObject.transform.Rotate(0, -rotspeed, 0);
            }
            else if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight))
            {
                PlayerObject.transform.Rotate(0, rotspeed, 0);
            }
        }
        //flont back rot
        if (birdseyeview == 1)
        {
            if (OVRInput.Get(OVRInput.RawButton.RThumbstickUp))
            {
                //
                //Vector3 forwardDirection = PlayerObject.forward;
                // x, y, z 方向に対して回転させたい角度を設定
                float rotationAmountX = 1f; // x 方向に回転する角度
                float rotationAmountY = 1f; // y 方向に回転する角度
                float rotationAmountZ = 1f;  // z 方向に回転する角度
                // オブジェクトの Transform に対して回転を適用する
                PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.right), rotationAmountX);
                //PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.up), rotationAmountY);
                //PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.forward), rotationAmountZ);
                //
                //PlayerObject.transform.Rotate(0, -rotspeed, 0);
            }
            if (OVRInput.Get(OVRInput.RawButton.RThumbstickDown))
            {
                // x, y, z 方向に対して回転させたい角度を設定
                float rotationAmountX = 1f; // x 方向に回転する角度
                float rotationAmountY = 1f; // y 方向に回転する角度
                float rotationAmountZ = 1f;  // z 方向に回転する角度
                // オブジェクトの Transform に対して回転を適用する
                PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.right), -rotationAmountX);
                //PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.up), rotationAmountY);
                //PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.forward), rotationAmountZ);
                //
                //PlayerObject.transform.Rotate(0, rotspeed, 0);
            }
        }
        //end of bev
        if (OVRInput.GetDown(OVRInput.RawButton.B) && birdseyeview == 1)
        {
            birdseyeview = 0;
            GameObject.Find("OVRPlayerController").transform.position = posiorigin + new Vector3(0,1,0);
            GameObject.Find("OVRPlayerController").transform.rotation = rotrigin;
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
        }
    }
    /*
    void BirdsEyeView()
    {
        GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
        //
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
        }
        //
        //右ジョイスティックの情報取得
        Vector2 stickR = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        Vector3 changePosition = new Vector3((stickR.x), 0, (stickR.y));
        //HMDのY軸の角度取得
        Vector3 changeRotation = new Vector3(0, InputTracking.GetLocalRotation(XRNode.Head).eulerAngles.y, 0);
        //OVRCameraRigの位置変更
        this.transform.position += this.transform.rotation * (Quaternion.Euler(changeRotation) * changePosition);
    }
    */
}