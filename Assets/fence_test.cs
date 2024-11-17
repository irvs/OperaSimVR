using UnityEngine;

public class Fence_test : MonoBehaviour
{
    public Transform objectA; // 1つ目のオブジェクト
    public Transform objectB; // 2つ目のオブジェクト
    private LineRenderer lineRenderer; // LineRendererコンポーネント
    // 線の幅（太さ）
    public float lineWidth = 0.2f;  // 線の幅（0.1f ～ 1.0f程度で調整）

    public float raycastDistance = 10f; // レイキャストの距離
    public LayerMask collisionLayer; // 衝突を検出するレイヤー
    string parentObjectName;
    public GameObject targetObject;
    Model_name name_space_of_model;

    void Start()
    {
        // LineRendererコンポーネントを取得
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // 初期位置に基づいてラインを描画
        UpdateLine();
    }

    void Update()
    {
        // オブジェクトの位置に基づいてラインを更新
        UpdateLine();

        // 衝突判定を行う
        CheckCollision();
    }

    void UpdateLine()
    {
        // LineRendererの2つの点をオブジェクトAとBの位置に設定
        lineRenderer.SetPosition(0, objectA.position);
        lineRenderer.SetPosition(1, objectB.position);
    }

    void CheckCollision()
    {
        // オブジェクトAとBを結ぶ直線をレイキャストとして扱う
        Vector3 direction = objectB.position - objectA.position;
        RaycastHit hit;

        // 直線（レイキャスト）を行い、衝突した場合の処理
        if (Physics.Raycast(objectA.position, direction.normalized, out hit, direction.magnitude, collisionLayer))
        {
            // 衝突した場合の処理
            //Debug.Log("衝突しました！衝突したオブジェクト: " + hit.collider.gameObject.name);
            // 例えば、衝突した位置を可視化するなど
            Debug.DrawLine(objectA.position, hit.point, Color.red);
            // 親オブジェクトの名前を取得
            Transform parentTransform = hit.collider.transform.root;
            parentObjectName = parentTransform.gameObject.name;
            if (parentTransform != null)
            {
                Debug.Log("衝突したオブジェクト: " + parentObjectName);
                //
                targetObject = GameObject.Find(parentObjectName);
                //targetObject = GameObject.Find("vr_cmd_vel_cont");
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
                    targetObject = GameObject.Find("vr_cmd_vel_cont");
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
