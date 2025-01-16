using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCameraRotation : MonoBehaviour
{
    public GameObject OVRCameraRig;
    public static float cameraRotationY = 0f;

    // Start is called before the first frame update
    void Start()
    {
        OVRCameraRig.transform.rotation = Quaternion.Euler(0, cameraRotationY + 90, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void SetRotationY(Transform trackingAnchorTransform)
    {
        cameraRotationY = trackingAnchorTransform.rotation.eulerAngles.y;
    }
}
