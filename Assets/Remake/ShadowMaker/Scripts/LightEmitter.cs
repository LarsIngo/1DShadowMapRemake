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

        public Color mColour;

        [Range(0, 360)]
        public float mAngle = 0;

        [Range(0,360)]
        public float mSpread = 180;

        public float mFalloffExponent = 1.0f;
        public float mAngleFalloffExponent = 1.0f;
        public float mFullBrightRadius = 0.0f;

        public float mRadius = 0.5f;

        private void Awake()
        {
            this.propertyBlock = new MaterialPropertyBlock();

            //MeshRenderer meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
            //if (meshRenderer.sharedMaterial == null)
            //{
            //    meshRenderer.sharedMaterial = new Material(ShadowRenderer.LoadShader("ShadowMaker/LightEmitter"));
            //}
        }

        private void OnValidate()
        {
            this.RebuildQuad();
        }

        private void OnEnable()
        {
            LightEmitter.AddEmitter(this);
        }

        private void OnDisable()
        {
            LightEmitter.RemoveEmitter(this);
        }

        private void OnDestroy()
        {
        }

        public float Angle
        {
            get
            {
                return mAngle;
            }
            set
            {
                if (mAngle != value)
                {
                    mAngle = value;

                    transform.localRotation = Quaternion.Euler(0.0f, 0.0f, mAngle);
                }
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
                if (mSpread != value)
                {
                    mSpread = value;
                    RebuildQuad();
                }
            }
        }

        public void Start()
        {
            mRadius = Mathf.Max(transform.localScale.x, transform.localScale.y) * 0.5f;

            transform.localScale = Vector3.one;

            RebuildQuad();
        }

        /// <summary>
        /// Build the light's quad mesh. This aims to fit the light cone as best as possible.
        /// 
        /// </summary>
        public void RebuildQuad()
        {
            Mesh m = GetComponent<MeshFilter>().sharedMesh;
            m = m ?? new Mesh();

            List<Vector3> verts = new List<Vector3>();

            if (mSpread > 180.0f)
            {
                verts.Add(new Vector3(-mRadius, -mRadius));
                verts.Add(new Vector3(+mRadius, +mRadius));
                verts.Add(new Vector3(+mRadius, -mRadius));
                verts.Add(new Vector3(-mRadius, +mRadius));
            }
            else
            {
                float radius = mRadius;

                float minAngle = -mSpread * 0.5f;
                float maxAngle = +mSpread * 0.5f;

                Bounds aabb = new Bounds(Vector3.zero, Vector3.zero);
                aabb.Encapsulate(new Vector3(radius, 0.0f));
                aabb.Encapsulate(new Vector3(Mathf.Cos(Mathf.Deg2Rad * minAngle), Mathf.Sin(Mathf.Deg2Rad * minAngle)) * radius);
                aabb.Encapsulate(new Vector3(Mathf.Cos(Mathf.Deg2Rad * maxAngle), Mathf.Sin(Mathf.Deg2Rad * maxAngle)) * radius);

                verts.Add(new Vector3(aabb.min.x, aabb.min.y));
                verts.Add(new Vector3(aabb.max.x, aabb.max.y));
                verts.Add(new Vector3(aabb.max.x, aabb.min.y));
                verts.Add(new Vector3(aabb.min.x, aabb.max.y));
            }

            m.SetVertices(verts);
        }

        public MaterialPropertyBlock BindShadowMap(RenderTexture shadowMapTexture)
        {
            Vector4 shadowMapParams = LightEmitter.GetShadowMapParams(this.shadowMapSlot);

            this.propertyBlock.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, mAngle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f));
            this.propertyBlock.SetVector("_ShadowMapParams", shadowMapParams);

            Material mat = this.gameObject.GetComponent<MeshRenderer>().sharedMaterial;

            float radius = mRadius;

            mat.SetVector("_Color", mColour);
            mat.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, mFalloffExponent, mAngleFalloffExponent));
            mat.SetVector("_Params2", new Vector4(mAngle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f, 1.0f / ((1.0f - mFullBrightRadius) * radius), mFullBrightRadius * radius));
            mat.SetVector("_ShadowMapParams", shadowMapParams);
            mat.SetTexture("_ShadowTex", shadowMapTexture);

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

    }
}
