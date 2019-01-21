using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LightBlocker : MonoBehaviour
    {
        private class LightMesh
        {
            private class Triangle
            {
                public int vertexIndexA;
                public int vertexIndexB;
                public int vertexIndexC;
                int[] vertices;

                public Triangle(int a, int b, int c)
                {
                    vertexIndexA = a;
                    vertexIndexB = b;
                    vertexIndexC = c;

                    vertices = new int[3];
                    vertices[0] = a;
                    vertices[1] = b;
                    vertices[2] = c;
                }

                public int this[int i]
                {
                    get
                    {
                        return vertices[i];
                    }
                }


                public bool Contains(int vertexIndex)
                {
                    return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
                }
            }


            private Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
            private List<List<int>> outlines = new List<List<int>>();
            private HashSet<int> checkedVertices = new HashSet<int>();

            public LightMesh(Mesh mesh)
            {
                this.triangleDictionary.Clear();
                this.outlines.Clear();
                this.checkedVertices.Clear();

                // Get indices.
                List<int> indices = new List<int>();
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
                {
                    Debug.Assert(mesh.GetTopology(subMeshIndex) == MeshTopology.Triangles);
                    mesh.GetIndices(indices, subMeshIndex);
                }

                // Get vertices.
                List<Vector3> vertices = new List<Vector3>();
                mesh.GetVertices(vertices);

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
                this.GenerateMesh(vertices);
            }

            void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
            {
                if (!triangleDictionary.ContainsKey(vertexIndexKey))
                {
                    triangleDictionary[vertexIndexKey] = new List<Triangle>();
                }

                triangleDictionary[vertexIndexKey].Add(triangle);
            }

            void CalculateMeshOutlines(List<Vector3> vertices)
            {
                for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
                {
                    if (!checkedVertices.Contains(vertexIndex))
                    {
                        int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                        if (newOutlineVertex != -1)
                        {
                            checkedVertices.Add(vertexIndex);

                            List<int> newOutline = new List<int>();
                            newOutline.Add(vertexIndex);
                            outlines.Add(newOutline);
                            FollowOutline(newOutlineVertex, outlines.Count - 1);
                            outlines[outlines.Count - 1].Add(vertexIndex);
                        }
                    }
                }
            }

            void FollowOutline(int vertexIndex, int outlineIndex)
            {
                outlines[outlineIndex].Add(vertexIndex);
                checkedVertices.Add(vertexIndex);
                int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

                if (nextVertexIndex != -1)
                {
                    FollowOutline(nextVertexIndex, outlineIndex);
                }
            }

            int GetConnectedOutlineVertex(int vertexIndex)
            {
                List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

                for (int i = 0; i < trianglesContainingVertex.Count; i++)
                {
                    Triangle triangle = trianglesContainingVertex[i];

                    for (int j = 0; j < 3; j++)
                    {
                        int vertexB = triangle[j];
                        if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                        {
                            if (IsOutlineEdge(vertexIndex, vertexB))
                            {
                                return vertexB;
                            }
                        }
                    }
                }

                return -1;
            }

            bool IsOutlineEdge(int vertexA, int vertexB)
            {
                List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
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

            Mesh GenerateMesh(List<Vector3> vertices)
            {
                List<Vector3> meshVertices = new List<Vector3>();
                List<int> meshIndices = new List<int>();

                foreach (List<int> outline in this.outlines)
                {
                    int startIndex = meshVertices.Count;
                    for (int i = 0; i < outline.Count - 1; ++i)
                    {
                        int offset = meshVertices.Count;
                        meshVertices.Add(vertices[outline[i]]);

                        meshIndices.Add(offset + 0);
                        meshIndices.Add(offset + 1);
                    }

                    meshIndices[meshIndices.Count - 1] = startIndex;
                }

                Mesh mesh = new Mesh();
                mesh.SetVertices(meshVertices);
                mesh.SetIndices(meshIndices.ToArray(), MeshTopology.Lines, 0);
                return mesh;
            }
        }


        private static List<LightBlocker> blockerList = new List<LightBlocker>();

        public static List<LightBlocker> GetActiveBlockerList()
        {
            return LightBlocker.blockerList;
        }

        private static void AddBlocker(LightBlocker blocker)
        {
            Debug.Assert(!LightBlocker.blockerList.Contains(blocker), typeof(LightBlocker).FullName + ".AddBlocker: Already in list.");
            LightBlocker.blockerList.Add(blocker);
        }
        
        private static void RemoveBlocker(LightBlocker blocker)
        {
            Debug.Assert(LightBlocker.blockerList.Contains(blocker), typeof(LightBlocker).FullName + ".RemoveBlocker: Not in list.");
            LightBlocker.blockerList.Remove(blocker);
        }

        public Mesh GetMesh()
        {
            return this.gameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
            LightBlocker.AddBlocker(this);
        }

        private void OnDisable()
        {
            LightBlocker.RemoveBlocker(this);
        }

        private void OnDestroy()
        {
        }

        public void GetEdges(List<Vector2> edges)
        {
            Vector3 v1 = new Vector3(-0.5f, -0.5f, 0.0f);
            Vector3 v2 = new Vector3(+0.5f, -0.5f, 0.0f);
            Vector3 v3 = new Vector3(+0.5f, +0.5f, 0.0f);
            Vector3 v4 = new Vector3(-0.5f, +0.5f, 0.0f);

            v1 = transform.localToWorldMatrix.MultiplyPoint(v1);
            v2 = transform.localToWorldMatrix.MultiplyPoint(v2);
            v3 = transform.localToWorldMatrix.MultiplyPoint(v3);
            v4 = transform.localToWorldMatrix.MultiplyPoint(v4);

            edges.Add(new Vector2(v1.x, v1.y));
            edges.Add(new Vector2(v2.x, v2.y));

            edges.Add(new Vector2(v2.x, v2.y));
            edges.Add(new Vector2(v3.x, v3.y));

            edges.Add(new Vector2(v3.x, v3.y));
            edges.Add(new Vector2(v4.x, v4.y));

            edges.Add(new Vector2(v4.x, v4.y));
            edges.Add(new Vector2(v1.x, v1.y));

            LightMesh lMesh = new LightMesh(this.GetMesh());
        }
    }
}
