namespace ShadowMaker
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Component which handles the rendering.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ShadowRenderer : MonoBehaviour
    {
        /// <summary>
        /// The resolution of the shadow map.
        /// </summary>
        public const int SHADOWMAPRESOLUTION = 1024;

        /// <summary>
        /// The maximum number of active emitters allowed at once.
        /// </summary>
        public const int EMITTERCOUNTMAX = 64;

        /// <summary>
        /// Mask used to allocate a row for an emitter.
        /// </summary>
        private static ulong emitterAllocMask = 0;

        /// <summary>
        /// The command buffer.
        /// </summary>
        private CommandBuffer commandBuffer;

        /// <summary>
        /// The material containing shader for rendering the initial shadow map.
        /// </summary>
        private Material shadowMapInitialMaterial;

        /// <summary>
        /// The render texture for the initial shadow map.
        /// </summary>
        [SerializeField]
        private RenderTexture shadowMapInitialRenderTexture;

        /// <summary>
        /// The material containing shader for rendering the final shadow map.
        /// </summary>
        private Material shadowMapFinalMaterial;

        /// <summary>
        /// The render texture for the final shadow map.
        /// </summary>
        [SerializeField]
        private RenderTexture shadowMapFinalRenderTexture;

        /// <summary>
        /// Gets the final shadow map.
        /// </summary>
        public static RenderTexture ShadowMapFinalRenderTexture
        {
            get
            {
                return UnityEngine.Object.FindObjectOfType<ShadowRenderer>().shadowMapFinalRenderTexture;
            }
        }

        /// <summary>
        /// Allocates a row of the shadow map for an emitter.
        /// </summary>
        /// <returns>The allocated slot.</returns>
        public static int AllocateEmitterSlot()
        {
            for (int i = 0; i < ShadowRenderer.EMITTERCOUNTMAX; i++)
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

        /// <summary>
        /// Frees a row of the shadow map from an emitter.
        /// </summary>
        /// <param name="slot">The slot to free.</param>
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

        /// <summary>
        /// Unity method triggered when Unity wakes game object.
        /// </summary>
        private void Awake()
        {
            Utility.GenerateInternalResources();

            this.commandBuffer = new CommandBuffer();
            Camera camera = this.gameObject.GetComponent<Camera>();
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, this.commandBuffer);

            // Initial shadow map in range 0-540.
            this.shadowMapInitialMaterial = new Material(Utility.LoadShader("ShadowMaker/ShadowMapInitial"));
            ////this.shadowMapInitialMaterial.renderQueue = (int)RenderQueue.Geometry; // 2000
            this.shadowMapInitialRenderTexture = new RenderTexture(Mathf.RoundToInt(SHADOWMAPRESOLUTION * 1.5f), EMITTERCOUNTMAX, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Default);
            this.shadowMapInitialRenderTexture.filterMode = FilterMode.Point;
            this.shadowMapInitialRenderTexture.wrapMode = TextureWrapMode.Repeat;
            this.shadowMapInitialRenderTexture.anisoLevel = 0;
            this.shadowMapInitialRenderTexture.autoGenerateMips = false;

            // Final shadow map in range 0-360.
            this.shadowMapFinalMaterial = new Material(Utility.LoadShader("ShadowMaker/ShadowMapFinal"));
            ////this.shadowMapFinalMaterial.renderQueue = (int)RenderQueue.Transparent; // 3000
            this.shadowMapFinalRenderTexture = new RenderTexture(SHADOWMAPRESOLUTION, EMITTERCOUNTMAX, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Default);
            this.shadowMapFinalRenderTexture.filterMode = FilterMode.Point;
            this.shadowMapFinalRenderTexture.wrapMode = TextureWrapMode.Repeat;
            this.shadowMapFinalRenderTexture.anisoLevel = 0;
            this.shadowMapFinalRenderTexture.autoGenerateMips = false;
        }

        /// <summary>
        /// Unity method triggered when Unity is about to render the scene.
        /// </summary>
        private void OnPreRender()
        {
            this.commandBuffer.Clear();
            this.commandBuffer.SetRenderTarget(this.shadowMapInitialRenderTexture);
            this.commandBuffer.ClearRenderTarget(false, true, new Color(1, 1, 1, 1));

            // Render shadow map range 0-540.
            List<LightBlocker> blockers = LightBlocker.GetActiveBlockerList();
            foreach (LightBlocker blocker in blockers)
            {
                Mesh blockerMesh = blocker.GetBlockerMesh();
                Matrix4x4 blockerMatrix = blocker.transform.localToWorldMatrix;

                List<LightEmitter> emitters = LightEmitter.GetActiveEmitterList();
                foreach (LightEmitter emitter in emitters)
                {
                    this.commandBuffer.DrawMesh(blockerMesh, blockerMatrix, this.shadowMapInitialMaterial, 0, -1, emitter.GetMaterialPropertyBlock());
                }
            }

            // Reduce shadow map range to 0-360.
            this.shadowMapFinalMaterial.SetTexture("_ShadowMap", this.shadowMapInitialRenderTexture);
            this.commandBuffer.SetRenderTarget(this.shadowMapFinalRenderTexture);
            this.commandBuffer.DrawMesh(Utility.FullQuadMesh(), Matrix4x4.identity, this.shadowMapFinalMaterial);
        }
    }
////#if UNITY_EDITOR
////    namespace Editor
////    {
////        [UnityEditor.CustomEditor(typeof(ShadowRenderer))]
////        public class ShadowRendererEditor : UnityEditor.Editor
////        {
////            public override void OnInspectorGUI()
////            {
////                ShadowRenderer target = (ShadowRenderer)this.target;
////
////                RenderTexture depthRenderTexture = target.GetDepthRenderTexture();
////                if (depthRenderTexture != null)
////                {
////                    // TODO, DISPLAY PREVIEW IN INSPECTOR.
////                    //Debug.Log("Draw");
////                    //Texture2D preview = UnityEditor.AssetPreview.GetAssetPreview(depthRenderTexture);
////                    //GUILayout.Label(preview);
////                    //UnityEditor.EditorGUILayout.ObjectField(preview, typeof(Texture2D));
////                }
////            }
////        }
////    }
////#endif
}
