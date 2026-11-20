using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Editor
{
    public sealed class ConePlacementWindow : EditorWindow
    {
        ConePlacementController controller;
        Vector2 scroll;
        bool editScene;

        [MenuItem("OperaSimVR/Cone Placement/配置線を編集")]
        public static void Open() => GetWindow<ConePlacementWindow>("コーン自動配置");

        void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGUI;
            Undo.undoRedoPerformed += UndoRedo;
            minSize = new Vector2(345, 440);
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            Undo.undoRedoPerformed -= UndoRedo;
            if (controller != null) controller.BeforeEdit = null;
        }

        void UndoRedo()
        {
            if (controller != null) controller.RefreshFromLayout();
            Repaint(); SceneView.RepaintAll();
        }

        void PrepareEdit()
        {
            if (Application.isPlaying || controller == null) return;
            Undo.RecordObject(controller, "Edit cone layout");
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        void OnGUI()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<ConePlacementController>();
                if (controller != null && !Application.isPlaying) controller.RefreshFromLayout();
            }
            GUILayout.Label("Sceneビューでコーン配置線を編集", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("地面クリックで頂点を追加します。IDを選んで隣接IDを指定し、グループごとの間隔を入力してください。Sceneで作成した配置はシーンに保存できます。Play中はF8からも編集できます。", MessageType.Info);
            controller = (ConePlacementController)EditorGUILayout.ObjectField("配置データ", controller, typeof(ConePlacementController), true);
            if (controller == null)
            {
                if (GUILayout.Button("このシーンにコーン配置機能を追加"))
                {
                    var obj = new GameObject("Cone Placement");
                    Undo.RegisterCreatedObjectUndo(obj, "Create cone placement");
                    controller = Undo.AddComponent<ConePlacementController>(obj);
                    Selection.activeGameObject = obj;
                    EditorSceneManager.MarkSceneDirty(obj.scene);
                }
                return;
            }
            controller.BeforeEdit = PrepareEdit;
            editScene = GUILayout.Toggle(editScene, "Sceneビューでクリック編集を有効にする", "Button");
            if (Application.isPlaying) EditorGUILayout.HelpBox("Play中の変更はJSON保存してください。停止するとシーン内の編集は元に戻ります。", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUIStyle oldLabel = GUI.skin.label;
            GUI.skin.label = new GUIStyle(oldLabel) { wordWrap = true };
            try
            {
                GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(300, position.width - 28)));
                controller.DrawControls();
                GUILayout.EndVertical();
            }
            finally { GUI.skin.label = oldLabel; }
            EditorGUILayout.EndScrollView();
        }

        void DuringSceneGUI(SceneView view)
        {
            if (controller == null) return;
            Event current = Event.current;
            if (editScene && current.type == EventType.Layout && !current.alt)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            foreach (ConeVertex vertex in controller.Layout.Vertices)
            {
                Vector3 point = vertex.Position + Vector3.up * .2f;
                Handles.color = vertex.Id == controller.SelectedId ? Color.yellow : new Color(.35f, 1f, .05f);
                float size = HandleUtility.GetHandleSize(point) * .08f;
                Handles.Label(point + Vector3.up * size, "P" + vertex.Id);
                if (editScene && !current.alt && Handles.Button(point, Quaternion.identity, size, size * 1.5f, Handles.SphereHandleCap))
                { controller.SelectVertex(vertex.Id); Repaint(); }
            }
            if (!editScene || current.alt || current.type != EventType.MouseDown || current.button != 0) return;
            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            if (controller.TryPickSurface(ray, out Vector3 position))
            {
                controller.HandleSceneClick(position);
                current.Use(); Repaint(); SceneView.RepaintAll();
            }
        }
    }

    [CustomEditor(typeof(ConePlacementController))]
    sealed class ConePlacementInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("頂点・接続・間隔は専用ウィンドウまたはPlay中のF8で編集します。メッシュ地面にはConePlacementSurfaceを追加してください。", MessageType.Info);
            if (GUILayout.Button("コーン配置ウィンドウを開く")) ConePlacementWindow.Open();
            SerializedProperty layers = serializedObject.FindProperty("surfaceLayers");
            serializedObject.Update();
            EditorGUILayout.PropertyField(layers, new GUIContent("地面のレイヤー"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
