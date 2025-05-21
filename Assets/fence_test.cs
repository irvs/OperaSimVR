using UnityEngine;

public class Fence_test : MonoBehaviour
{
    public Transform objectA; // 1つ目のオブジェクト
    public Transform objectB; // 2つ目のオブジェクト
    private LineRenderer lineRenderer; // LineRendererコンポーネント
    public LayerMask collisionLayer; // 衝突を検出するレイヤー
    string parentObjectName;
    private GameObject targetObject;

    void Start()
    {
        // LineRendererコンポーネントを取得
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        UpdateLine();
    }

    void Update()
    {
        UpdateLine();
        CheckCollision();
    }

    void UpdateLine()
    {
        lineRenderer.SetPosition(0, objectA.position);
        lineRenderer.SetPosition(1, objectB.position);
    }

    void CheckCollision()
    {
        Vector3 direction = objectB.position - objectA.position;
        RaycastHit hit;

        // 直線（レイキャスト）を行い、衝突した場合の処理
        if (Physics.Raycast(objectA.position, direction.normalized, out hit, direction.magnitude, collisionLayer))
        {
            // 衝突した場合の処理
            Debug.DrawLine(objectA.position, hit.point, Color.red);
            // 親オブジェクトの名前を取得
            Transform parentTransform = hit.collider.transform.root;
            parentObjectName = parentTransform.gameObject.name;
            if (parentTransform != null)
            {
                Debug.Log("衝突したオブジェクト: " + parentObjectName);
                //
                targetObject = GameObject.Find(parentObjectName);
                VR_cont_2 scriptA = targetObject.GetComponent<VR_cont_2>();
                if (scriptA != null)
                {
                    scriptA.emergency = true;
                    Debug.Log("emergency");
                }
                Model_name scriptB = targetObject.GetComponent<Model_name>();
                if (scriptB != null && scriptB.ObjectTypeIsPaperMachine == true) 
                {
                    VR_cont_2 scriptC = GameObject.Find(scriptB.ParentMachine).GetComponent<VR_cont_2>();
                    if (scriptC != null)
                    {
                        scriptC.emergency = true;
                        Debug.Log("emergency");
                    }
                }
                /*
                if (parentObjectName == "ic120")
                {
                    // targetObject = GameObject.Find("ic120");
                    Debug.Log("fence " + targetObject);
                    VR_cont_2 scriptA = targetObject.GetComponent<VR_cont_2>();
                    scriptA.emergency = true;
                    Debug.Log("emergency");
                }
                */
                //
            }
        }
        else
        {
            // 衝突しなかった場合
            Debug.DrawLine(objectA.position, objectB.position, Color.green);
        }

    }
}
