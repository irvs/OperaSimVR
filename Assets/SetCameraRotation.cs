using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCameraRotation : MonoBehaviour
{
    public GameObject OVRCameraRig;
    public static float cameraRotationY = 0f;
    public float AngularSpeed = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        OVRCameraRig.transform.rotation = Quaternion.Euler(0, cameraRotationY + 90, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            OVRCameraRig.transform.Rotate(0, - AngularSpeed, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            OVRCameraRig.transform.Rotate(0, AngularSpeed, 0);
        }
    }

    public static void SetRotationY(Transform trackingAnchorTransform)
    {
        cameraRotationY = trackingAnchorTransform.rotation.eulerAngles.y;
    }
}
