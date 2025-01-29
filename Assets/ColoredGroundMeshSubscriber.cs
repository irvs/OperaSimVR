using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.TmsMsgDb;

public class ColoredGroundMeshSubscriber : MonoBehaviour
{
    public string topicName = "/output/map_2d/mesh";

    private ColorRGBAMsg[] colorRGBAMsgs;
    private Color[] vertexColors;
    public static ColoredMeshMsg coloredMeshMsg;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<ColoredMeshMsg>(topicName, UpdateColoredMesh);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GetVertexColors()
    {
        vertexColors = new Color[colorRGBAMsgs.Length];
        for (int i = 0; i < colorRGBAMsgs.Length; i++)
        {
            vertexColors[i] = new Color(colorRGBAMsgs[i].r, colorRGBAMsgs[i].g, colorRGBAMsgs[i].b);
        }
    }

    private void UpdateColoredMesh(ColoredMeshMsg msg)
    {
        coloredMeshMsg = msg;
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;

        colorRGBAMsgs = msg.vertex_colors;

        GetVertexColors();

        mesh.SetColors(vertexColors);
        meshFilter.mesh = mesh;
    }
}
