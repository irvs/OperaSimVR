using UnityEngine;

public class ApplyMultipleMaterials : MonoBehaviour
{
    public Material[] materials; // 複数のマテリアルをインスペクターで設定

    void Start()
    {
        // Rendererコンポーネントを取得
        Renderer renderer = GetComponent<Renderer>();

        // 複数のマテリアルを設定
        if (renderer != null && materials.Length > 0)
        {
            renderer.materials = materials;  // 複数のマテリアルを設定
        }
        else
        {
            Debug.LogError("Rendererが見つかりません！またはマテリアルが設定されていません！");
        }
    }
}
