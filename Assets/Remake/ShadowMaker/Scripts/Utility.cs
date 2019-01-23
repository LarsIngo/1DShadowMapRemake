using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaker
{
    public static class Utility
    {
        private static Mesh fullQuadMesh;

        private static Mesh halfQuadMesh;

        public static void GenerateInternalResources()
        {
            Utility.FullQuadMesh();
            Utility.HalfQuadMesh();
        }

        public static Shader LoadShader(string name)
        {
            Shader shader = Shader.Find(name);
            Debug.Assert(shader != null, typeof(ShadowRenderer).FullName + ".LoadShader: Shader (" + name + ") is null.");
            Debug.Assert(shader.isSupported, typeof(ShadowRenderer).FullName + ".LoadShader: Shader (" + name + ") not supported.");
            return shader;
        }

        public static Mesh FullQuadMesh()
        {
            if (Utility.fullQuadMesh == null)
            {
                List<Vector3> verts = new List<Vector3>();
                List<Vector2> uvs0 = new List<Vector2>();
                int[] indices = new int[6];

                verts.Add(new Vector3(-1.0f, +1.0f, 0.0f));
                verts.Add(new Vector3(+1.0f, +1.0f, 0.0f));
                verts.Add(new Vector3(+1.0f, -1.0f, 0.0f));
                verts.Add(new Vector3(-1.0f, -1.0f, 0.0f));

                uvs0.Add(new Vector2(0.0f, 0.0f));
                uvs0.Add(new Vector2(1.0f, 0.0f));
                uvs0.Add(new Vector2(1.0f, 1.0f));
                uvs0.Add(new Vector2(0.0f, 1.0f));

                indices[0] = 0;
                indices[1] = 1;
                indices[2] = 2;
                indices[3] = 0;
                indices[4] = 2;
                indices[5] = 3;

                Mesh mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs0);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                Utility.fullQuadMesh = mesh;
            }

            return Utility.fullQuadMesh;
        }

        public static Mesh HalfQuadMesh()
        {
            if (Utility.halfQuadMesh == null)
            {
                List<Vector3> verts = new List<Vector3>();
                List<Vector2> uvs0 = new List<Vector2>();
                int[] indices = new int[6];

                verts.Add(new Vector3(+0.0f, +1.0f, 0.0f));
                verts.Add(new Vector3(+1.0f, +1.0f, 0.0f));
                verts.Add(new Vector3(+1.0f, -1.0f, 0.0f));
                verts.Add(new Vector3(+0.0f, -1.0f, 0.0f));

                uvs0.Add(new Vector2(0.0f, 0.0f));
                uvs0.Add(new Vector2(1.0f, 0.0f));
                uvs0.Add(new Vector2(1.0f, 1.0f));
                uvs0.Add(new Vector2(0.0f, 1.0f));

                indices[0] = 0;
                indices[1] = 1;
                indices[2] = 2;
                indices[3] = 0;
                indices[4] = 2;
                indices[5] = 3;

                Mesh mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs0);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                Utility.halfQuadMesh = mesh;
            }

            return Utility.halfQuadMesh;
        }
    }
//#if UNITY_EDITOR
//    namespace Editor
//    {
//        [UnityEditor.CustomEditor(typeof(ShadowRenderer))]
//        public class ShadowRendererEditor : UnityEditor.Editor
//        {
//            public override void OnInspectorGUI()
//            {
//                ShadowRenderer target = (ShadowRenderer)this.target;

//                RenderTexture depthRenderTexture = target.GetDepthRenderTexture();
//                if (depthRenderTexture != null)
//                {
//                    // TODO, DISPLAY PREVIEW IN INSPECTOR.
//                    //Debug.Log("Draw");
//                    //Texture2D preview = UnityEditor.AssetPreview.GetAssetPreview(depthRenderTexture);
//                    //GUILayout.Label(preview);
//                    //UnityEditor.EditorGUILayout.ObjectField(preview, typeof(Texture2D));
//                }
//            }
//        }
//    }
//#endif
}
