using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class GameLight : ShadowCaster {

    public Color        mColour;
    private float       mAngle = 0;
    private float       mSpread = 360;
    public float        mFalloffExponent = 1.0f;
    public float        mAngleFalloffExponent = 1.0f;
	public float 		mFullBrightRadius = 0.0f;
    private float       mRadius = 0.0f;

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
        Mesh m = GetComponent<MeshFilter>().mesh;

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

    /// <summary>
    /// This function sets up the parameters needed to DRAW the shadow map, plus the parameters to USE the shadow map when this object is rendered.
    /// Returns null if wanting to skip shadow rendering.
    /// </summary>
    /// <param name="shadowMapTexture"></param>
    /// <returns></returns>
    public override MaterialPropertyBlock BindShadowMap(Texture shadowMapTexture)
    {
        Vector4 shadowMapParams = GetShadowMapParams(mShadowMapSlot);

        mMaterialPropertyBlock.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, mAngle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f));
        mMaterialPropertyBlock.SetVector("_ShadowMapParams", shadowMapParams);

		MeshRenderer mr = GetComponent<MeshRenderer> ();

        Material mat = mr.materials[0];

        float radius = mRadius;

        mat.SetVector("_Color", mColour);
		mat.SetVector("_LightPosition", new Vector4(transform.position.x, transform.position.y, mFalloffExponent, mAngleFalloffExponent));
		mat.SetVector("_Params2", new Vector4(mAngle * Mathf.Deg2Rad, mSpread * Mathf.Deg2Rad * 0.5f, 1.0f / ((1.0f - mFullBrightRadius) * radius), mFullBrightRadius * radius));
        mat.SetVector("_ShadowMapParams", shadowMapParams);
        mat.SetTexture("_ShadowTex", shadowMapTexture);
			
        return mMaterialPropertyBlock;
    }
}
