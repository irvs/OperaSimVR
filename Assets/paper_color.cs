using UnityEngine;

public class ParentColorChanger : MonoBehaviour
{
    // 親オブジェクトから子オブジェクトを取得して色を変更する
    public Color newColor = Color.red;

    void Start()
    {
        foreach (Transform child in transform)
        {
            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.material.color = newColor;
            }
            Debug.Log("Color have changed. : " + gameObject.name);
        }
        /*
        // 親オブジェクトから"ChildObjectName"という名前の子オブジェクトを取得
        //Transform childTransform = transform.Find("ChildObjectName");


        if (childTransform != null)
        {
            // 子オブジェクトのRendererコンポーネントを取得
            Renderer childRenderer = childTransform.GetComponent<Renderer>();

            if (childRenderer != null)
            {
                // 子オブジェクトの色を変更
                childRenderer.material.color = newColor;
            }
            else
            {
                Debug.LogWarning("Renderer component not found on child object.");
            }
        }
        else
        {
            Debug.LogWarning("Child object not found.");
        }
        */
    }
}
