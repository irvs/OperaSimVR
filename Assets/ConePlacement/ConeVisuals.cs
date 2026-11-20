using System.Collections.Generic;
using UnityEngine;

namespace OperaSimVR.ConePlacement
{
    internal static class ConeVisuals
    {
        // Shared mesh: square dark foot, orange taper and white reflective band. Origin is the ground contact.
        public static Mesh CreateConeMesh()
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            Color orange = new Color(1f, .3f, .025f);
            Color white = new Color(.98f, .98f, .9f);
            Color black = new Color(.12f, .14f, .15f);
            Vector3[] bottom = { new Vector3(-.24f, 0, -.24f), new Vector3(.24f, 0, -.24f),
                new Vector3(.24f, 0, .24f), new Vector3(-.24f, 0, .24f) };
            AddQuad(bottom[3] + Vector3.up * .045f, bottom[2] + Vector3.up * .045f,
                bottom[1] + Vector3.up * .045f, bottom[0] + Vector3.up * .045f, black, vertices, colors, indices);
            for (int i = 0; i < 4; i++)
                AddQuad(bottom[i], bottom[(i + 1) % 4], bottom[(i + 1) % 4] + Vector3.up * .045f,
                    bottom[i] + Vector3.up * .045f, black, vertices, colors, indices);
            float[] levels = { .045f, .28f, .42f, .7f };
            for (int band = 0; band < levels.Length - 1; band++)
            {
                float low = levels[band], high = levels[band + 1];
                for (int i = 0; i < 20; i++)
                {
                    float a = 2f * Mathf.PI * i / 20, b = 2f * Mathf.PI * (i + 1) / 20;
                    float r0 = Mathf.Lerp(.19f, .015f, (low - .045f) / .655f);
                    float r1 = Mathf.Lerp(.19f, .015f, (high - .045f) / .655f);
                    Color tint = (band == 1 ? white : orange) * (.72f + .28f * (.5f + .5f * Mathf.Cos(a)));
                    tint.a = 1;
                    AddQuad(new Vector3(Mathf.Cos(a) * r0, low, Mathf.Sin(a) * r0),
                        new Vector3(Mathf.Cos(a) * r1, high, Mathf.Sin(a) * r1),
                        new Vector3(Mathf.Cos(b) * r1, high, Mathf.Sin(b) * r1),
                        new Vector3(Mathf.Cos(b) * r0, low, Mathf.Sin(b) * r0), tint, vertices, colors, indices);
                }
            }
            var mesh = new Mesh { name = "Cone placement shared cone" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color,
            List<Vector3> vertices, List<Color> colors, List<int> indices)
        {
            int n = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            colors.AddRange(new[] { color, color, color, color });
            indices.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }
    }
}
