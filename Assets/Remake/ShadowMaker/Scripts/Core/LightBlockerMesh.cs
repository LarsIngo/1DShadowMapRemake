namespace ShadowMaker.Core
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Class used to generate mesh which is used to generate shadow map.
    /// </summary>
    public class LightBlockerMesh
    {
        /// <summary>
        /// Dictionary mapping from vertex index to triangles.
        /// </summary>
        private Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();

        /// <summary>
        /// List containing outline of mesh.
        /// </summary>
        private List<List<int>> outlines = new List<List<int>>();

        /// <summary>
        /// Hash set used to check which vertices that have are already been checked.
        /// </summary>
        private HashSet<int> checkedVertices = new HashSet<int>();

        /// <summary>
        /// The render mesh.
        /// </summary>
        private Mesh renderMesh = null;

        /// <summary>
        /// The blocker mesh.
        /// </summary>
        private Mesh blockerMesh = null;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="renderMesh">The render mesh.</param>
        public LightBlockerMesh(Mesh renderMesh)
        {
            this.UpdateBlockerMesh(renderMesh);
        }

        /// <summary>
        /// Update blocker mesh.
        /// </summary>
        /// <param name="renderMesh">The new render mesh which a blocker mesh is generated from.</param>
        public void UpdateBlockerMesh(Mesh renderMesh)
        {
            UnityEngine.Object.DestroyImmediate(this.blockerMesh);

            this.renderMesh = renderMesh;

            this.triangleDictionary.Clear();
            this.outlines.Clear();
            this.checkedVertices.Clear();

            // Get indices.
            List<int> indices = new List<int>();
            for (int subMeshIndex = 0; subMeshIndex < renderMesh.subMeshCount; ++subMeshIndex)
            {
                Debug.Assert(renderMesh.GetTopology(subMeshIndex) == MeshTopology.Triangles, this.GetType().FullName + ".UpdateBlockerMesh: Render mesh needs to be a triangle mesh");
                renderMesh.GetIndices(indices, subMeshIndex);
            }

            // Get vertices.
            List<Vector3> vertices = new List<Vector3>();
            renderMesh.GetVertices(vertices);

            // Create triangles.
            List<Triangle> triangles = new List<Triangle>();
            for (int i = 0; i < indices.Count; i += 3)
            {
                triangles.Add(new Triangle(indices[i + 0], indices[i + 1], indices[i + 2]));
            }

            // Create triangle dictionary.
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                foreach (Triangle triangle in triangles)
                {
                    if (triangle.Contains(vertexIndex))
                    {
                        this.AddTriangleToDictionary(vertexIndex, triangle);
                    }
                }
            }

            // Fill outlines.
            this.CalculateMeshOutlines(vertices);

            // Create mesh from outlines.
            this.blockerMesh = this.GenerateBlockerMesh(vertices);
        }

        /// <summary>
        /// Gets the render mesh.
        /// </summary>
        /// <returns>The render mesh.</returns>
        public Mesh GetRenderMesh()
        {
            return this.renderMesh;
        }

        /// <summary>
        /// Gets the blocker mesh.
        /// </summary>
        /// <returns>The blocker mesh.</returns>
        public Mesh GetBlockerMesh()
        {
            return this.blockerMesh;
        }

        /// <summary>
        /// Adds a triangle to the list inside the dictionary.
        /// </summary>
        /// <param name="vertexIndexKey">The vertex index used as key.</param>
        /// <param name="triangle">The triangle to add.</param>
        private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
        {
            if (!this.triangleDictionary.ContainsKey(vertexIndexKey))
            {
                this.triangleDictionary[vertexIndexKey] = new List<Triangle>();
            }

            this.triangleDictionary[vertexIndexKey].Add(triangle);
        }

        /// <summary>
        /// Methods which populates the outline list.
        /// </summary>
        /// <param name="vertices">The vertices of the mesh.</param>
        private void CalculateMeshOutlines(List<Vector3> vertices)
        {
            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                if (!this.checkedVertices.Contains(vertexIndex))
                {
                    int newOutlineVertex = this.GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex != -1)
                    {
                        this.checkedVertices.Add(vertexIndex);

                        List<int> newOutline = new List<int>();
                        newOutline.Add(vertexIndex);
                        this.outlines.Add(newOutline);
                        this.FollowOutline(newOutlineVertex, this.outlines.Count - 1);
                        this.outlines[this.outlines.Count - 1].Add(vertexIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Method which follows an outline. 
        /// </summary>
        /// <param name="vertexIndex">The current vertex index.</param>
        /// <param name="outlineIndex">The current outline index.</param>
        private void FollowOutline(int vertexIndex, int outlineIndex)
        {
            this.outlines[outlineIndex].Add(vertexIndex);
            this.checkedVertices.Add(vertexIndex);
            int nextVertexIndex = this.GetConnectedOutlineVertex(vertexIndex);

            if (nextVertexIndex != -1)
            {
                this.FollowOutline(nextVertexIndex, outlineIndex);
            }
        }

        /// <summary>
        /// Method used to find the connecting outline vertex.
        /// </summary>
        /// <param name="vertexIndex">The vertex to find connected outline vertex for.</param>
        /// <returns>The connected outline vertex.</returns>
        private int GetConnectedOutlineVertex(int vertexIndex)
        {
            List<Triangle> trianglesContainingVertex = this.triangleDictionary[vertexIndex];

            for (int i = 0; i < trianglesContainingVertex.Count; i++)
            {
                Triangle triangle = trianglesContainingVertex[i];

                for (int j = 0; j < 3; j++)
                {
                    int vertexB = triangle[j];
                    if (vertexB != vertexIndex && !this.checkedVertices.Contains(vertexB))
                    {
                        if (this.IsOutlineEdge(vertexIndex, vertexB))
                        {
                            return vertexB;
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Method used to check whether two vertices are on the outline.
        /// </summary>
        /// <param name="vertexA">The first vertex.</param>
        /// <param name="vertexB">The second vertex.</param>
        /// <returns>Whether the vertices are on the outline.</returns>
        private bool IsOutlineEdge(int vertexA, int vertexB)
        {
            List<Triangle> trianglesContainingVertexA = this.triangleDictionary[vertexA];
            int sharedTriangleCount = 0;

            for (int i = 0; i < trianglesContainingVertexA.Count; i++)
            {
                if (trianglesContainingVertexA[i].Contains(vertexB))
                {
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1)
                    {
                        break;
                    }
                }
            }

            return sharedTriangleCount == 1;
        }

        /// <summary>
        /// Method used to generate a blocker mesh.
        /// </summary>
        /// <param name="vertices">The list of vertices.</param>
        /// <returns>The blocker mesh.</returns>
        private Mesh GenerateBlockerMesh(List<Vector3> vertices)
        {
            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector2> meshNeighbors = new List<Vector2>();
            List<int> meshIndices = new List<int>();

            foreach (List<int> outline in this.outlines)
            {
                int count = outline.Count;
                for (int i = 0; i < count; ++i)
                {
                    meshIndices.Add(meshIndices.Count);
                    meshVertices.Add(vertices[outline[i + 0]]);
                    meshNeighbors.Add(vertices[outline[(i + 1) % count]]);

                    meshIndices.Add(meshIndices.Count);
                    meshVertices.Add(vertices[outline[(i + 1) % count]]);
                    meshNeighbors.Add(vertices[outline[i + 0]]);
                }
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(meshVertices);
            mesh.SetUVs(0, meshNeighbors);
            mesh.SetIndices(meshIndices.ToArray(), MeshTopology.Lines, 0);
            return mesh;
        }

        /// <summary>
        /// Internal class which represents a triangle containing three vertices.
        /// </summary>
        private class Triangle
        {
            /// <summary>
            /// The first vertex.
            /// </summary>
            private int a;

            /// <summary>
            /// The second vertex.
            /// </summary>
            private int b;

            /// <summary>
            /// The third vertex.
            /// </summary>
            private int c;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="a">The first vertex.</param>
            /// <param name="b">The second vertex.</param>
            /// <param name="c">The third vertex.</param>
            public Triangle(int a, int b, int c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }

            /// <summary>
            /// Operator used to get vertex by index.
            /// </summary>
            /// <param name="i">The index.</param>
            /// <returns>The vertex.</returns>
            public int this[int i]
            {
                get
                {
                    switch (i)
                    {
                        case 0:
                            return this.a;
                        case 1:
                            return this.b;
                        case 2:
                            return this.c;
                        default:
                            return int.MaxValue;
                    }
                }
            }

            /// <summary>
            /// Check whether vertex is contained in triangle.
            /// </summary>
            /// <param name="vertexIndex">The vertex to check.</param>
            /// <returns>Whether vertex is contained in triangle.</returns>
            public bool Contains(int vertexIndex)
            {
                return vertexIndex == this.a || vertexIndex == this.b || vertexIndex == this.c;
            }
        }
    }
}