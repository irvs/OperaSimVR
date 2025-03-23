using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapToWorld : MonoBehaviour
{
    public GameObject TargetObject;
    public float MapRefetenceX;
    public float MapRefetenceY;
    public float MapRefetenceZ;
    private Vector3 ReferencePointPose;
    private Quaternion ReferencePointRot;
    private Quaternion ModifyMapRot;
    private Vector3 MapPosition;
    private Vector3 ModifyMapPosition;
    private Vector3 WorldPosition;
    private Vector3 ModifyWorldPosition;
    public float awaitingdebugTimestamp;
    public float requestInterval = 5.0f;
    public bool DebugSw;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ReferencePointPose = GameObject.Find("map_Reference point").transform.position;
        ReferencePointRot = GameObject.Find("map_Reference point").transform.rotation;
        MapPosition = TargetObject.transform.position - ReferencePointPose;
        ModifyMapPosition = new Vector3(MapPosition.x, MapPosition.z, 0.0f);
        WorldPosition = MapPosition + new Vector3(MapRefetenceX, MapRefetenceZ, MapRefetenceY);
        ModifyWorldPosition = new Vector3(WorldPosition.z, WorldPosition.x, WorldPosition.y);
        ModifyMapRot = new Quaternion(TargetObject.transform.rotation.x, TargetObject.transform.rotation.z, TargetObject.transform.rotation.w, TargetObject.transform.rotation.y);

        if (Time.time > awaitingdebugTimestamp && DebugSw == true)
        {
            Debug.Log(MapPosition);
            Debug.Log(ModifyMapPosition);
           // Debug.Log(WorldPosition);
            Debug.Log(ModifyWorldPosition);
            Debug.Log(ModifyMapRot);
            awaitingdebugTimestamp = Time.time + requestInterval;
        }
    }
}
