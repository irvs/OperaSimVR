using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class position_speaker : MonoBehaviour
{
    PoseSubscriber RealPosition;
    public Vector3 newPosition;
    public Quaternion newRotation;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RealPosition = GetComponent<PoseSubscriber>();
        newPosition = RealPosition.newPosition;
        newRotation = RealPosition.newRotation;
        Debug.Log(newPosition);
        Debug.Log(newRotation);
    }
}
