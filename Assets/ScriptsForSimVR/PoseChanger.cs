using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseChanger : MonoBehaviour
{
    PoseSubscriber MachinePoseSubscriber;
    public GameObject SubscriberObject;
    GameObject targetObject;
    public float HeightOffset = 100.0f;
    GameObject PlayerObject;
    ControllerManager ControllerManager;

    // Start is called before the first frame update
    void Start()
    {
        MachinePoseSubscriber = SubscriberObject.GetComponent<PoseSubscriber>();
        targetObject = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        targetObject.transform.position = SubscriberObject.transform.position + new Vector3(0, HeightOffset, 0);
        targetObject.transform.rotation = SubscriberObject.transform.rotation;
    }
}
