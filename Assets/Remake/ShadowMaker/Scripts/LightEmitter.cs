﻿using System.Collections;
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

        [Range(0,360)]
        public float mSpread = 180;

        public float mFalloffExponent = 1.0f;
        public float mAngleFalloffExponent = 1.0f;
        public float mFullBrightRadius = 0.0f;

        public float mRadius = 0.5f;

        private void Awake()
        {
            this.propertyBlock = new MaterialPropertyBlock();

            MeshRenderer meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer.sharedMaterial == null)
            {
                Material material = new Material(ShadowRenderer.LoadShader("ShadowMaker/LightEmitter"));
                ////material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background; // 1000
                meshRenderer.sharedMaterial = material;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            GizmosDrawIcon();
            GizmosDrawArc(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            GizmosDrawIcon();
            GizmosDrawArc(1.0f);
            GizmosDrawCircle(1.0f);
        }

        private void GizmosDrawArc(float alpha)
        {
            float radius = Mathf.Max(this.transform.localScale.x, this.transform.localScale.y);
            UnityEditor.Handles.color = new Color(this.mColour.r, this.mColour.g, this.mColour.b, alpha);
            UnityEditor.Handles.DrawSolidArc(this.transform.position, Vector3.forward, Quaternion.Euler(0, 0, -this.mSpread * 0.5f) * this.transform.right, this.mSpread, radius);
        }

        private void GizmosDrawCircle(float alpha)
        {
            float radius = Mathf.Max(this.transform.localScale.x, this.transform.localScale.y);
            UnityEditor.Handles.color = new Color(this.mColour.r, this.mColour.g, this.mColour.b, alpha);
            UnityEditor.Handles.DrawWireDisc(this.transform.position, Vector3.forward, radius);
        }

        private void GizmosDrawIcon()
        {
            Gizmos.color = new Color(0,1,0,1);
            Gizmos.DrawIcon(this.transform.position, "Light Icon", true);
        }
#endif

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
            //float maxScale = Mathf.Max(transform.localScale.x, transform.localScale.y);
            //transform.localScale = new Vector3(maxScale, maxScale, 1.0f);

            //RebuildQuad();

            this.gameObject.GetComponent<MeshFilter>().sharedMesh = this.Spread > 180.0f ? ShadowRenderer.FullQuadMesh() : ShadowRenderer.HalfQuadMesh();
        }

        public float Angle
        {
            get
            {
                return transform.localRotation.eulerAngles.z;
            }
            set
            {
                transform.localRotation = Quaternion.Euler(0.0f, 0.0f, value);
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
                }
            }
        }

        public float LightScale
        {
            get
            {
                return Mathf.Max(transform.localScale.x, transform.localScale.y);
            }
        }

        public void Start()
        {
            //mRadius = Mathf.Max(transform.localScale.x, transform.localScale.y) * 0.5f;
            //mRadius = 2.5f;

            //transform.localScale = Vector3.one;

            //RebuildQuad();
        }

        public MaterialPropertyBlock BindShadowMap(RenderTexture shadowMapTexture)
        {
            Vector4 shadowMapParams = LightEmitter.GetShadowMapParams(this.shadowMapSlot);

            float angle = this.Angle;

            this.propertyBlock.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, angle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f));
            this.propertyBlock.SetVector("_ShadowMapParams", shadowMapParams);

            Material mat = this.gameObject.GetComponent<MeshRenderer>().sharedMaterial;

            float radius = mRadius * 5 * 2;

            mat.SetVector("_Color", mColour);
            mat.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, mFalloffExponent, mAngleFalloffExponent));
            mat.SetVector("_Params2", new Vector4(angle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f, 1.0f / ((1.0f - mFullBrightRadius) * radius), mFullBrightRadius * radius));
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
