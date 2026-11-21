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

        [MenuItem("OperaSimVR/Cone Placement/Edit Layout")]
        public static void Open() => GetWindow<ConePlacementWindow>("Cone Placement");

        void OnEnable()
        {
            titleContent = new GUIContent("Cone Placement");
            SceneView.duringSceneGui += DuringSceneGUI;
            Undo.undoRedoPerformed += UndoRedo;
            EditorApplication.update += PollRosService;
            AssemblyReloadEvents.beforeAssemblyReload += CancelRosService;
            minSize = new Vector2(345, 440);
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            Undo.undoRedoPerformed -= UndoRedo;
            EditorApplication.update -= PollRosService;
            AssemblyReloadEvents.beforeAssemblyReload -= CancelRosService;
            CancelRosService();
            if (controller != null) controller.BeforeEdit = null;
        }

        void CancelRosService() { if (controller != null) controller.CancelRosService(); }
        void PollRosService()
        {
            if (controller == null || !controller.IsSendingToRos) return;
            controller.PollRosService();
            Repaint();
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
            GUILayout.Label("Cone Placement in Scene View", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Add points on the ground, connect their IDs, then place cones. Save the scene with Ctrl+S. In Play Mode, press F8 to edit.", MessageType.Info);
            var selected = (ConePlacementController)EditorGUILayout.ObjectField("Layout", controller, typeof(ConePlacementController), true);
            if (selected != controller)
            {
                if (controller != null) { controller.CancelRosService(); controller.BeforeEdit = null; }
                controller = selected;
            }
            if (controller == null)
            {
                if (GUILayout.Button("Add Cone Placement to Scene"))
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
            editScene = GUILayout.Toggle(editScene, "Enable Scene Click Editing", "Button");
            if (Application.isPlaying) EditorGUILayout.HelpBox("Save Play Mode changes to JSON before stopping. Scene changes are reset when Play Mode stops.", MessageType.Info);
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
            EditorGUILayout.HelpBox("Edit points, connections, and spacing in the Cone Placement window, or press F8 in Play Mode. Add ConePlacementSurface to mesh ground.", MessageType.Info);
            if (GUILayout.Button("Open Cone Placement")) ConePlacementWindow.Open();
            SerializedProperty layers = serializedObject.FindProperty("surfaceLayers");
            serializedObject.Update();
            EditorGUILayout.PropertyField(layers, new GUIContent("Ground Layers"));
            SerializedProperty ros = serializedObject.FindProperty("rosService");
            EditorGUILayout.PropertyField(ros, new GUIContent("ROS 2 Service"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
