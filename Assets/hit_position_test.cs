using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainCollisionPoint : MonoBehaviour
{

    public OVRRayHelper ovrRayHelper;  // OVRRayHelperの参照
    public Transform handAnchor;       // 手の位置（HandAnchor）
    public Terrain terrain;            // 衝突したいTerrain
    public float rayLength = 10f;      // Rayの長さ（デフォルト10ユニット）
    private Vector3 collisionPoint;
    Controller_manager Geton_controller_manager;
    List<Vector3> PathForDB = new List<Vector3>();

    public Transform startPoint;  // 始点
    public Transform endPoint;    // 終点

    public LineRenderer lineRenderer;  // LineRendererの参照


    void Start()
    {

        // LineRendererが設定されていない場合、エラーを防ぐ
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // LineRendererの初期設定
        lineRenderer.positionCount = 0;  // 2点間を結ぶライン
        lineRenderer.startWidth = 0.05f; // ラインの太さ（始点）
        lineRenderer.endWidth = 0.05f;   // ラインの太さ（終点）
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // ラインのマテリアル
        lineRenderer.startColor = Color.red; // ラインの始点の色
        lineRenderer.endColor = Color.red;   // ラインの終点の色
    }


    void Update()
    {
        // 手の位置（HandAnchor）をRayの始点として使用
        Vector3 rayOrigin = handAnchor.position;  // HandAnchorの位置

        // Rayの方向は手の向き（handAnchor.forward）を使用
        Vector3 rayDirection = handAnchor.forward; // HandAnchorの前方向を使う

        // Rayを作成
        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;

        // Raycastを飛ばしてTerrainと衝突した場合
        if (Physics.Raycast(ray, out hit, rayLength))
        {
            // 衝突対象がTerrainかどうかを確認
            if (hit.collider.CompareTag("Terrain"))
            {
                // 衝突位置の座標を表示
                collisionPoint = hit.point;
                Debug.Log("衝突位置: " + collisionPoint);
            }
        }

        Geton_controller_manager = this.GetComponent<Controller_manager>();
        if ((Geton_controller_manager.GetOnMachine == 0 && OVRInput.Get(OVRInput.RawButton.RIndexTrigger)) || (Geton_controller_manager.GetOnMachine == 0 && Input.GetKeyDown(KeyCode.B)))
        {
            PathForDB.Add(collisionPoint);
            lineRenderer.positionCount += 1;
            Debug.Log("add points: " + collisionPoint);

            if (PathForDB.Count >= 2)
            {
                Debug.Log("ready to commbine points");
                ConnectRender(PathForDB[PathForDB.Count - 2], collisionPoint);
                Debug.Log("commbine points");
            }
            
        }
        if (PathForDB.Count >= 2)
        {
            Vector3 direction = collisionPoint - PathForDB[PathForDB.Count - 2];

            // Rayを作成（始点と方向を指定）
            Ray rays = new Ray(PathForDB[PathForDB.Count - 2], direction);

            // Rayを可視化（デバッグ用）
            Debug.DrawRay(PathForDB[PathForDB.Count - 2], direction, Color.red);

            // Raycastを飛ばして衝突するかを確認
            RaycastHit hits;
            if (Physics.Raycast(rays, out hits))
            {
                // 衝突した場合、衝突点を表示
                Debug.Log("衝突した位置: " + hits.point);
            }
        }

    }

    void ConnectRender(Vector3 startPosition, Vector3 endPosition)
    {
        // LineRendererを更新して、Rayを描画

        // 各点の位置をLineRendererに設定
        for (int i = 0; i < PathForDB.Count; i++)
        {
            lineRenderer.SetPosition(i, PathForDB[i]);
        }

    }


        void ConnectRay(Vector3 origin, Vector3 target)
    {
        // 始点から終点への方向を計算
        Vector3 direction = target - origin;

        // Rayを作成（始点と方向を指定）
        Ray ray = new Ray(origin, direction);

        // Rayを可視化（デバッグ用）
        Debug.DrawRay(origin, direction, Color.red);

        // Raycastを飛ばして衝突するかを確認
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // 衝突した場合、衝突点を表示
            Debug.Log("衝突した位置: " + hit.point);
        }
    }



}
