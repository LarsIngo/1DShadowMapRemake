using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public class ShadowCaster : MonoBehaviour {

    public int mShadowMapSlot = -1;

    public MaterialPropertyBlock mMaterialPropertyBlock;

	public static List<ShadowCaster> ShadowCasterPool = new List<ShadowCaster>();

    public virtual void Awake()
    {
        mMaterialPropertyBlock = new MaterialPropertyBlock();
    }

    public virtual void OnEnable()
    {
        mShadowMapSlot = ShadowMapAlloc();
		ShadowCasterPool.Add(this);
    }

	public virtual void OnDisable()
    {
		ShadowCasterPool.Remove(this);
        if (mShadowMapSlot >= 0)
        {
            ShadowMapFree(mShadowMapSlot);
            mShadowMapSlot = -1;
        }
    }

    public virtual MaterialPropertyBlock BindShadowMap(Texture shadowMapTexture)
    {
        return null;
    }

    /// <summary>
    /// Allocator row rows in the shadow map
    /// </summary>

    static ulong ShadowMapAllocator = 0;

    public const int MAX_SHADOW_MAPS = 64;      // this must match the height of the shadow map texture

    /// <summary>
    // Allocate a shadow map row for a 1D shadow map
    /// </summary>
    /// <returns></returns>
    public static int ShadowMapAlloc()
    {
        for (int i = 0; i < MAX_SHADOW_MAPS; i++)
        {
            ulong mask = (ulong)1 << i;
            if ((ShadowMapAllocator & mask) == 0)
            {
                ShadowMapAllocator |= mask;
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Free up a shadow map row
    /// </summary>
    /// <param name="slot"></param>
    public static void ShadowMapFree(int slot)
    {
        if (slot >= 0)
        {
            ulong mask = (ulong)1 << slot;
            Debug.Assert((ShadowMapAllocator & mask) != 0);
            ShadowMapAllocator &= ~mask;
        }
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
        float u1 = ((float)slot + 0.5f) / MAX_SHADOW_MAPS;
        float u2 = (u1 - 0.5f) * 2.0f;

        if (   //(SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2) // OpenGL2 is no longer supported in Unity 5.5+
               (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
            || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
            || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
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
