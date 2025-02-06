using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayController : MonoBehaviour
{
    public Transform anchor;
    public float maxDistance = 10f;
    public LineRenderer line;
    public List<string> sensorpods = new List<string>{
        "SensorPod_v3_1",
        "SensorPod_v3_2",
        "SensorPod_v3_3",
        "SensorPod_v3_4",
    };
    public Material defaultSensorPodMaterial;
    public Material selectedSensorPodMaterial;

    private string selectedSensorPod = "";

    // scale
    private float scale_big = 1.5f;

    // rainbow
    private float initial = 0f;
    private float duration = 1f;

    // Start is called before the first frame update
    void Start()
    {

    }

    void Update()
    {
        RaycastHit hit;
        Ray ray = new Ray(anchor.position, anchor.forward);

        // レーザーの起点
        line.SetPosition(0, ray.origin);

        // 3. rainbow
        float theta = (initial + Time.time) / duration * 10;
        float amp = (Mathf.Cos(theta) + 1.0f) / 2;

        // DEBUG: 手元確認用
        // GameObject nowTarget = GameObject.Find("SensorPod_v3_1");
        // nowTarget.GetComponent<Renderer>().materials[1].color = Color.HSVToRGB(amp, 1, 1);

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            // レーザーの終点（オブジェクトにぶつかった場合）
            // line.SetPosition(1, hit.point);

            // レーザーが当たったオブジェクトを取得
            GameObject target = hit.collider.gameObject;

            // if target name is in sensorpods
            if (sensorpods.IndexOf(target.name) >= 0)
            {
                // scale up with 3 times

                // 1. change scale
                target.transform.localScale = new Vector3(scale_big, scale_big, scale_big);

                // 2. change material
                // Material[] meshRendererMaterials = target.GetComponent<MeshRenderer>().materials;
                // meshRendererMaterials[1] = selectedSensorPodMaterial;
                // target.GetComponent<MeshRenderer>().materials = meshRendererMaterials;

                // 3. rainbow
                target.GetComponent<Renderer>().materials[1].color = Color.HSVToRGB(amp, 1, 1);

                // register selected sensorpod name
                selectedSensorPod = target.name;
            }
            else if (selectedSensorPod != "")
            {
                GameObject prevTarget = GameObject.Find(selectedSensorPod);

                // 1. change scale
                prevTarget.transform.localScale = new Vector3(1, 1, 1);

                // 2. change material, 3. rainbow
                Material[] meshRendererMaterials = prevTarget.GetComponent<MeshRenderer>().materials;
                meshRendererMaterials[1] = defaultSensorPodMaterial;
                prevTarget.GetComponent<MeshRenderer>().materials = meshRendererMaterials;

                selectedSensorPod = "";
            }
        }
        else
        {
            // // レーザーの終点（何にもぶつからなかった場合）
            if (selectedSensorPod != "")
            {
                GameObject target = GameObject.Find(selectedSensorPod);

                // 1. change scale
                target.transform.localScale = new Vector3(1, 1, 1);

                // 2. change material, 3. rainbow
                Material[] meshRendererMaterials = target.GetComponent<MeshRenderer>().materials;
                meshRendererMaterials[1] = defaultSensorPodMaterial;
                target.GetComponent<MeshRenderer>().materials = meshRendererMaterials;

                selectedSensorPod = "";
            }
        }
    }
}