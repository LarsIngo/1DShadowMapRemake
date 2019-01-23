namespace ShadowMaker
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Component which emits light.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LightEmitter : MonoBehaviour
    {
        /// <summary>
        /// List of all the active emitters in the scene.
        /// </summary>
        private static List<LightEmitter> emitterList = new List<LightEmitter>();

        /// <summary>
        /// Color of the light.
        /// </summary>
        [SerializeField]
        private Color color = new Color(0.2f, 0.72f, 0.2f, 1.0f);

        /// <summary>
        /// Spread of the light (in degrees).
        /// </summary>
        [SerializeField]
        [Range(0, 360)]
        private float spread = 180;

        /// <summary>
        /// Falloff exponent of the light (range).
        /// </summary>
        [SerializeField]
        [Range(0, 20)]
        private float falloffExponent = 1.0f;

        /// <summary>
        /// Falloff exponent of the light (angle).
        /// </summary>
        [SerializeField]
        [Range(0, 20)]
        private float angleFalloffExponent = 1.0f;

        /// <summary>
        /// Full brightness radius.
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        private float fullBrightRadius = 0.0f;

        /// <summary>
        /// Which row this emitter is allocated in the shadow map.
        /// </summary>
        private int shadowMapSlot = -1;

        /// <summary>
        /// A block of materials values to apply.
        /// </summary>
        private MaterialPropertyBlock propertyBlock;

        /// <summary>
        /// Gets the angle of emitter (in degrees).
        /// </summary>
        public float Angle
        {
            get
            {
                return transform.rotation.eulerAngles.z;
            }
        }

        /// <summary>
        /// Gets or sets the spread of emitter (in degrees).
        /// </summary>
        public float Spread
        {
            get
            {
                return spread;
            }

            set
            {
                spread = value;
            }
        }

        /// <summary>
        /// Gets the radius of the emitter.
        /// </summary>
        public float Radius
        {
            get
            {
                return this.transform.lossyScale.x;
            }
        }

        /// <summary>
        /// Calculate the parameters used to read and write the 1D shadow map.
        /// x = parameter for reading shadow map (UV space (0,1)).
        /// y = parameter for writing shadow map (clip space (-1,+1)).
        /// </summary>
        /// <param name="slot">The slot allocated.</param>
        /// <returns>The coordinates to write/read the correct row in the shadow map.</returns>
        public static Vector4 GetShadowMapParams(int slot)
        {
            float u1 = ((float)slot + 0.5f) / ShadowRenderer.EMITTERCOUNTMAX;
            float u2 = (u1 - 0.5f) * 2.0f;

            if (   ////(SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2) // OpenGL2 is no longer supported in Unity 5.5+
                   (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore)
                || (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2)
                || (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3))
            {
                return new Vector4(u1, u2, 0.0f, 0.0f);
            }
            else
            {
                return new Vector4(1.0f - u1, u2, 0.0f, 0.0f);
            }
        }

        /// <summary>
        /// Gets the list of active emitters.
        /// </summary>
        /// <returns>The list of active emitters.</returns>
        public static List<LightEmitter> GetActiveEmitterList()
        {
            return LightEmitter.emitterList;
        }

        /// <summary>
        /// Get the material property block.
        /// </summary>
        /// <returns>The material property block.</returns>
        public MaterialPropertyBlock GetMaterialPropertyBlock()
        {
            float angle = this.Angle;
            float radius = this.Radius;

            // ShadowMapInitial.shader
            this.propertyBlock.SetVector("_EmitterParams", new Vector4(transform.position.x, transform.position.y, angle * Mathf.Deg2Rad, radius));
            this.propertyBlock.SetVector("_ShadowMapParams", LightEmitter.GetShadowMapParams(this.shadowMapSlot));

            return this.propertyBlock;
        }

        /// <summary>
        /// Adds a emitter to the active emitter list.
        /// </summary>
        /// <param name="emitter">The emitter to add.</param>
        private static void AddEmitter(LightEmitter emitter)
        {
            Debug.Assert(emitter.gameObject.activeInHierarchy, typeof(LightBlocker).FullName + ".AddEmitter: Emitter is not active.");
            Debug.Assert(!LightEmitter.emitterList.Contains(emitter), typeof(LightBlocker).FullName + ".AddEmitter: Already in list.");
            emitter.shadowMapSlot = ShadowRenderer.AllocateEmitterSlot();
            LightEmitter.emitterList.Add(emitter);
        }

        /// <summary>
        /// Removes a emitter from the active emitter list.
        /// </summary>
        /// <param name="emitter">The emitter to remove.</param>
        private static void RemoveEmitter(LightEmitter emitter)
        {
            Debug.Assert(!emitter.gameObject.activeInHierarchy, typeof(LightBlocker).FullName + ".RemoveEmitter: Emitter is active.");
            Debug.Assert(LightEmitter.emitterList.Contains(emitter), typeof(LightBlocker).FullName + ".RemoveEmitter: Not in list.");
            ShadowRenderer.FreeEmitterSlot(ref emitter.shadowMapSlot);
            LightEmitter.emitterList.Remove(emitter);
        }

        /// <summary>
        /// Updates the transform based on emitter properties.
        /// </summary>
        private void UpdateTransform()
        {
            this.gameObject.GetComponent<MeshFilter>().sharedMesh = this.Spread > 180.0f ? Utility.FullQuadMesh() : Utility.HalfQuadMesh(); // Get either half or full quad depending on spread.
            float yScale = (this.Spread > 180.0f ? 1.0f : Mathf.Sin(this.Spread * 0.5f * Mathf.Deg2Rad)) * this.Radius; // Calculate local y scale to fit spread and radius.
            float parentScaleFactor = this.transform.lossyScale.y / this.transform.localScale.y; // Remove parent scale in y in order for light mesh to scale propely in y when childed.
            this.transform.localScale = new Vector3(this.transform.localScale.x, Mathf.Max(yScale / parentScaleFactor, 0.001f), 1.0f); // Update scale in order to fit mesh to light spread and radius.
        }

        /// <summary>
        /// Unity method triggered when Unity wakes game object.
        /// </summary>
        private void Awake()
        {
            this.propertyBlock = new MaterialPropertyBlock();

            MeshRenderer meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer.sharedMaterial == null)
            {
                Material material = new Material(Utility.LoadShader("ShadowMaker/LightEmitter"));
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background; // 1000
                meshRenderer.sharedMaterial = material;
            }

            this.UpdateTransform();
        }

        /// <summary>
        /// Unity method called when game object is enabled.
        /// </summary>
        private void OnEnable()
        {
            LightEmitter.AddEmitter(this);
        }

        /// <summary>
        /// Unity method called when game object is disabled.
        /// </summary>
        private void OnDisable()
        {
            LightEmitter.RemoveEmitter(this);
        }

        /// <summary>
        /// Unity method called when game object will be rendered.
        /// </summary>
        private void OnWillRenderObject()
        {
            this.UpdateTransform();

            // LightEmitter.shader
            float angle = this.Angle;
            float radius = this.Radius;
            Material mat = this.gameObject.GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetVector("_Color", color);
            mat.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, falloffExponent, angleFalloffExponent));
            mat.SetVector("_Params2", new Vector4(angle * Mathf.Deg2Rad, spread * Mathf.Deg2Rad * 0.5f, 1.0f / ((1.0f - fullBrightRadius) * radius), fullBrightRadius * radius));
            mat.SetVector("_LightRadius", new Vector4(radius, 0.0f, 0.0f, 0.0f));
            mat.SetVector("_ShadowMapParams", LightEmitter.GetShadowMapParams(this.shadowMapSlot));
            mat.SetTexture("_ShadowMap", ShadowRenderer.ShadowMapFinalRenderTexture);
            mat.SetVector("_ShadowMapResolution", new Vector4(ShadowRenderer.SHADOWMAPRESOLUTION, 0.0f, 0.0f, 0.0f));
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity method triggered when gizmos are drawn.
        /// </summary>
        private void OnDrawGizmos()
        {
            GizmosDrawIcon();
            GizmosDrawArc(0.05f);
        }

        /// <summary>
        /// Unity method triggered when a gizmos is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            GizmosDrawIcon();
            GizmosDrawArc(0.20f);
            GizmosDrawCircle(0.40f);
        }

        /// <summary>
        /// Draws an arc.
        /// </summary>
        /// <param name="alpha">The alpha value.</param>
        private void GizmosDrawArc(float alpha)
        {
            UnityEditor.Handles.color = new Color(this.color.r, this.color.g, this.color.b, alpha);
            UnityEditor.Handles.DrawSolidArc(this.transform.position, Vector3.forward, Quaternion.Euler(0, 0, -this.spread * 0.5f) * this.transform.right, this.spread, this.Radius);
        }

        /// <summary>
        /// Draws a circle.
        /// </summary>
        /// <param name="alpha">The alpha value.</param>
        private void GizmosDrawCircle(float alpha)
        {
            UnityEditor.Handles.color = new Color(this.color.r, this.color.g, this.color.b, alpha);
            UnityEditor.Handles.DrawWireDisc(this.transform.position, Vector3.forward, this.Radius);
        }

        /// <summary>
        /// Draws an icon.
        /// </summary>
        private void GizmosDrawIcon()
        {
            Gizmos.color = new Color(0, 1, 0, 1);
            Gizmos.DrawIcon(this.transform.position, "Light Icon", true);
        }
#endif
    }
}
