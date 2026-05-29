using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelIdentifier : MonoBehaviour
{
    public GameObject targetObject;
    public bool ObjectTypeIsPaperMachine;
    public string ParentMachine;
    public GameObject PaperMachine;
    public string Modelname;
    //public string KindsOfHeavyMachinery;
    public enum HeavyMachineryOption { ZX120, ZX200, IC120, C30R}
    public HeavyMachineryOption KindsOfHeavyMachinery;
    public float Offset_x = 0;
    public float Offset_y = 0;
    public float Offset_z = 0;
    public float OffsetRotation_x = 0;
    public float OffsetRotation_y = 0;
    public float OffsetRotation_z = 0;
    public List<float> OffsetList = new List<float>();

    // Start is called before the first frame update
    void Start()
    {
        targetObject = this.gameObject;
        Modelname = targetObject.name;
        OffsetList.Add(0.0f);
        OffsetList.Add(0.0f);
        OffsetList.Add(0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        OffsetList[0] = Offset_x + OffsetRotation_x;
        OffsetList[1] = Offset_y + OffsetRotation_y;
        OffsetList[2] = Offset_z + OffsetRotation_z;
    }
}
