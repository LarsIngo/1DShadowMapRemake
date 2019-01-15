using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowMaker
{
    [RequireComponent(typeof(Camera))]
    public class ShadowRenderer : MonoBehaviour
    {
        private CommandBuffer commandBuffer;

        private Mesh screenQuadMesh;

        private Material shadowMapInitialMaterial;

        [SerializeField] // TODO use Custom Editor.
        private RenderTexture shadowMapInitialRenderTexture;

        private Material shadowMapFinalMaterial;

        [SerializeField] // TODO use Custom Editor.
        private RenderTexture shadowMapFinalRenderTexture;

        public const int SHADOWMAP_RESOLUTION = 640;

        // --- LIGHTEMITTER --- //
        public const int EMITTER_COUNT_MAX = 64;

        private static ulong emitterAllocMask = 0;
        
        // --- Utility --- //
        public static int AllocateEmitterSlot()
        {
            for (int i = 0; i < EMITTER_COUNT_MAX; i++)
            {
                ulong mask = (ulong)1 << i;
                if ((ShadowRenderer.emitterAllocMask & mask) == 0)
                {
                    ShadowRenderer.emitterAllocMask |= mask;
                    return i;
                }
            }

            Debug.LogError(typeof(ShadowRenderer).FullName + ".AllocateEmitterSlot: Unable to allocate slot.");
            return -1;
        }

        public static void FreeEmitterSlot(ref int slot)
        {
            if (slot >= 0)
            {
                ulong mask = 1UL << slot;
                Debug.Assert((ShadowRenderer.emitterAllocMask & mask) != 0, typeof(ShadowRenderer).FullName + ".FreeEmitterSlot: Slot is not allocated.");
                ShadowRenderer.emitterAllocMask &= ~mask;
                slot = -1;
            }
        }

        public static Shader LoadShader(string name)
        {
            Shader shader = Shader.Find(name);
            Debug.Assert(shader != null, typeof(ShadowRenderer).FullName + ".LoadShader: Shader (" + name + ") is null.");
            Debug.Assert(shader.isSupported, typeof(ShadowRenderer).FullName + ".LoadShader: Shader (" + name + ") not supported.");
            return shader;
        }

        private static Mesh CreateScreenQuad()
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

            return mesh;
        }

        // --- Unity --- //
        private void Awake()
        {
            this.commandBuffer = new CommandBuffer();
            Camera camera = this.gameObject.GetComponent<Camera>();
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, this.commandBuffer);

            this.screenQuadMesh = ShadowRenderer.CreateScreenQuad();

            // Initial shadow map in range 0-540.
            this.shadowMapInitialMaterial = new Material(ShadowRenderer.LoadShader("ShadowMaker/ShadowMapInitial"));
            ////this.shadowMapInitialMaterial.renderQueue = (int)RenderQueue.Geometry; // 2000
            this.shadowMapInitialRenderTexture = new RenderTexture(Mathf.RoundToInt(SHADOWMAP_RESOLUTION * 1.5f), EMITTER_COUNT_MAX, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Default);
            this.shadowMapInitialRenderTexture.filterMode = FilterMode.Point;

            // Final shadow map in range 0-360.
            this.shadowMapFinalMaterial = new Material(ShadowRenderer.LoadShader("ShadowMaker/ShadowMapFinal"));
            ////this.shadowMapFinalMaterial.renderQueue = (int)RenderQueue.Transparent; // 3000
            this.shadowMapFinalRenderTexture = new RenderTexture(SHADOWMAP_RESOLUTION, EMITTER_COUNT_MAX, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Default);
            this.shadowMapFinalRenderTexture.filterMode = FilterMode.Point;
        }

        private void OnPreRender()
        {
            Mesh lightBlockerMesh = ShadowRenderer.CreateLightBlockerMesh();

            if (lightBlockerMesh != null)
            {
                this.commandBuffer.Clear();
                this.commandBuffer.SetRenderTarget(this.shadowMapInitialRenderTexture);
                this.commandBuffer.ClearRenderTarget(true, true, new Color(1, 1, 1, 1), 1.0f);

                List<LightEmitter> emitters = LightEmitter.GetActiveEmitterList();
                foreach (LightEmitter emitter in emitters)
                {
                    MaterialPropertyBlock properties = emitter.BindShadowMap(this.shadowMapFinalRenderTexture);
                    if (properties != null)
                    {
                        this.commandBuffer.DrawMesh(lightBlockerMesh, Matrix4x4.identity, this.shadowMapInitialMaterial, 0, -1, properties);
                    }
                }

                // Reduce shadow map range to 0-360.
                this.shadowMapFinalMaterial.SetTexture("_ShadowMap", this.shadowMapInitialRenderTexture);
                this.commandBuffer.SetRenderTarget(this.shadowMapFinalRenderTexture);
                this.commandBuffer.DrawMesh(this.screenQuadMesh, Matrix4x4.identity, this.shadowMapFinalMaterial);
            }
        }

        private void OnPostRender()
        {
        }

        // Create a mesh containing all the light blocker edges
        private static Mesh CreateLightBlockerMesh()
        {
            List<LightBlocker> blockers = LightBlocker.GetActiveBlockerList();

            if (blockers.Count == 0)
            {
                return null;
            }

            List<Vector2> edges = new List<Vector2>();
            foreach (LightBlocker blocker in blockers)
            {
                blocker.GetEdges(edges);
            }

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> normals = new List<Vector2>();
            for (int i = 0; i < edges.Count; i += 2)
            {
                verts.Add(edges[i + 0]);
                verts.Add(edges[i + 1]);
                normals.Add(edges[i + 1]);
                normals.Add(edges[i + 0]);
            }

            // Simple 1:1 index buffer
            int[] incides = new int[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                incides[i] = i;
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, normals);
            mesh.SetIndices(incides, MeshTopology.Lines, 0);

            return mesh;
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
