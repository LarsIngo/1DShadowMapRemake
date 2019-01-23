namespace ShadowMaker
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Static class containing utility methods.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Full quad mesh.
        /// </summary>
        private static Mesh fullQuadMesh;

        /// <summary>
        /// Half quad mesh.
        /// </summary>
        private static Mesh halfQuadMesh;

        /// <summary>
        /// Generates internal resources.
        /// </summary>
        public static void GenerateInternalResources()
        {
            Utility.FullQuadMesh();
            Utility.HalfQuadMesh();
        }

        /// <summary>
        /// Loads a shader based on name.
        /// </summary>
        /// <param name="name">The name of the shader.</param>
        /// <returns>The shader.</returns>
        public static Shader LoadShader(string name)
        {
            Shader shader = Shader.Find(name);
            Debug.Assert(shader != null, typeof(ShadowRenderer).FullName + ".LoadShader: Shader (" + name + ") is null.");
            Debug.Assert(shader.isSupported, typeof(ShadowRenderer).FullName + ".LoadShader: Shader (" + name + ") not supported.");
            return shader;
        }

        /// <summary>
        /// Gets full quad mesh.
        /// The mesh is generated if it does not exist.
        /// </summary>
        /// <returns>The full quad mesh.</returns>
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

        /// <summary>
        /// Gets half quad mesh.
        /// The mesh is generated if it does not exist.
        /// </summary>
        /// <returns>The half quad mesh.</returns>
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
}
