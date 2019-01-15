using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LightEmitter : MonoBehaviour
    {
        private static List<LightEmitter> emitterList = new List<LightEmitter>();

        public static List<LightEmitter> GetActiveEmitterList()
        {
            return LightEmitter.emitterList;
        }

        private static void AddEmitter(LightEmitter emitter)
        {
            Debug.Assert(!LightEmitter.emitterList.Contains(emitter), typeof(LightBlocker).FullName + ".AddEmitter: Already in list.");
            emitter.shadowMapSlot = ShadowRenderer.AllocateEmitterSlot();
            LightEmitter.emitterList.Add(emitter);
        }

        private static void RemoveEmitter(LightEmitter emitter)
        {
            Debug.Assert(LightEmitter.emitterList.Contains(emitter), typeof(LightBlocker).FullName + ".RemoveEmitter: Not in list.");
            ShadowRenderer.FreeEmitterSlot(ref emitter.shadowMapSlot);
            LightEmitter.emitterList.Remove(emitter);
        }

        private int shadowMapSlot = -1;

        private MaterialPropertyBlock propertyBlock;

        public Color mColour = new Color(0.2f, 0.72f, 0.2f, 1.0f);

        [Range(0,360)]
        public float mSpread = 180;

        [Range(0, 20)]
        public float mFalloffExponent = 1.0f;

        [Range(0, 20)]
        public float mAngleFalloffExponent = 1.0f;

        [Range(0, 1)]
        public float mFullBrightRadius = 0.0f;

        private void Awake()
        {
            this.propertyBlock = new MaterialPropertyBlock();

            MeshRenderer meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer.sharedMaterial == null)
            {
                Material material = new Material(ShadowRenderer.LoadShader("ShadowMaker/LightEmitter"));
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background; // 1000
                meshRenderer.sharedMaterial = material;
            }
        }

        private void OnEnable()
        {
            LightEmitter.AddEmitter(this);
        }

        private void OnDisable()
        {
            LightEmitter.RemoveEmitter(this);
        }

        private void OnWillRenderObject()
        {
            this.gameObject.GetComponent<MeshFilter>().sharedMesh = this.Spread > 180.0f ? ShadowRenderer.FullQuadMesh() : ShadowRenderer.HalfQuadMesh(); // Get either half or full quad depending on spread.
            float yScale = (this.Spread > 180.0f ? 1.0f : Mathf.Sin(this.Spread * 0.5f * Mathf.Deg2Rad)) * this.Radius; // Calculate local y scale to fit spread and radius.
            float parentScaleFactor = this.transform.lossyScale.y / this.transform.localScale.y; // Remove parent scale in y in order for light mesh to scale propely in y when childed.
            this.transform.localScale = new Vector3(this.transform.localScale.x, Mathf.Max(yScale / parentScaleFactor, 0.001f), 1.0f); // Update scale in order to fit mesh to light spread and radius.
        }

        public float Angle
        {
            get
            {
                return transform.rotation.eulerAngles.z;
            }
        }

        public float Spread
        {
            get
            {
                return mSpread;
            }
            set
            {
                mSpread = value;
            }
        }

        public float Radius
        {
            get
            {
                return this.transform.lossyScale.x;
            }
        }

        public MaterialPropertyBlock BindShadowMap(Texture shadowMap, Texture shadowMapBlurred)
        {
            Vector4 shadowMapParams = LightEmitter.GetShadowMapParams(this.shadowMapSlot);

            float angle = this.Angle;

            this.propertyBlock.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, angle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f));
            this.propertyBlock.SetVector("_ShadowMapParams", shadowMapParams);
            this.propertyBlock.SetVector("_LightRadius", new Vector4(this.Radius, 0, 0, 0));

            Material mat = this.gameObject.GetComponent<MeshRenderer>().material;

            float radius = this.Radius;

            mat.SetVector("_Color", mColour);
            mat.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, mFalloffExponent, mAngleFalloffExponent));
            mat.SetVector("_Params2", new Vector4(angle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f, 1.0f / ((1.0f - mFullBrightRadius) * radius), mFullBrightRadius * radius));
            mat.SetVector("_LightRadius", new Vector4(radius, 0, 0, 0));
            mat.SetVector("_ShadowMapParams", shadowMapParams);
            mat.SetTexture("_ShadowMap", shadowMap);
            mat.SetTexture("_ShadowMapBlurred", shadowMapBlurred);

            return this.propertyBlock;
        }

        /// <summary>
        /// calculate the parameters used to read and write the 1D shadow map.
        /// x = parameter for reading shadow map (uv space (0,1))
        /// y = parameter for writing shadow map (clip space (-1,+1))
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static Vector4 GetShadowMapParams(int slot)
        {
            float u1 = ((float)slot + 0.5f) / ShadowRenderer.EMITTER_COUNT_MAX;
            float u2 = (u1 - 0.5f) * 2.0f;

            if (   //(SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2) // OpenGL2 is no longer supported in Unity 5.5+
                   (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore)
                || (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2)
                || (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                )
            {
                return new Vector4(u1, u2, 0.0f, 0.0f);
            }
            else
            {
                return new Vector4(1.0f - u1, u2, 0.0f, 0.0f);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            GizmosDrawIcon();
            GizmosDrawArc(0.05f);
        }

        private void OnDrawGizmosSelected()
        {
            GizmosDrawIcon();
            GizmosDrawArc(0.20f);
            GizmosDrawCircle(0.40f);
        }

        private void GizmosDrawArc(float alpha)
        {
            UnityEditor.Handles.color = new Color(this.mColour.r, this.mColour.g, this.mColour.b, alpha);
            UnityEditor.Handles.DrawSolidArc(this.transform.position, Vector3.forward, Quaternion.Euler(0, 0, -this.mSpread * 0.5f) * this.transform.right, this.mSpread, this.Radius);
        }

        private void GizmosDrawCircle(float alpha)
        {
            UnityEditor.Handles.color = new Color(this.mColour.r, this.mColour.g, this.mColour.b, alpha);
            UnityEditor.Handles.DrawWireDisc(this.transform.position, Vector3.forward, this.Radius);
        }

        private void GizmosDrawIcon()
        {
            Gizmos.color = new Color(0, 1, 0, 1);
            Gizmos.DrawIcon(this.transform.position, "Light Icon", true);
        }
#endif
    }
}
