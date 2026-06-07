using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelIdentifier : MonoBehaviour
{
    public enum HeavyMachineryOption { ZX120, ZX200, IC120, C30R}
    public HeavyMachineryOption KindsOfHeavyMachinery;
    public float Offset_x = 0;
    public float Offset_y = 0;
    public float Offset_z = 0;
    public float OffsetRotation_x = 0;
    public float OffsetRotation_y = 0;
    public float OffsetRotation_z = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
