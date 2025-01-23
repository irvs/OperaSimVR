using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Model_name : MonoBehaviour
{
    public GameObject targetObject;
    public bool ObjectTypeIsPaperMachine;
    public string ParentMachine;
    public string Modelname;
    //public string KindsOfHeavyMachinery;
    public enum HeavyMachineryOption { ZX120, ZX200, IC120, C30R}
    public HeavyMachineryOption KindsOfHeavyMachinery;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    public float offset_adoptor_x = 0;
    public float offset_adoptor_y = 0;
    public float offset_adoptor_z = 0;
    public List<float> OffsetList = new List<float>();
    // Start is called before the first frame update
    void Start()
    {
        OffsetList.Add(0.0f);
        OffsetList.Add(0.0f);
        OffsetList.Add(0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        OffsetList[0] = offset_x + offset_adoptor_x;
        OffsetList[1] = offset_y + offset_adoptor_y;
        OffsetList[2] = offset_z + offset_adoptor_z;
    }
}
