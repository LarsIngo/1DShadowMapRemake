Shader "ShadowMaker/ShadowMapFinal"
{
    Properties
    {
        [PerRendererData] _ShadowMap ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ShadowMap1D.cginc"

            #define DEPTH_BIAS 0.001f
            
            struct appdata_t
            {
                float4 vertex    : POSITION;
                float2 texcoords : TEXCOORD0; 
            };

            appdata_t vert(appdata_t i)
            {
                return i;
            }

            sampler2D _ShadowMap;

            fixed4 frag(appdata_t i) : SV_Target
            {
                float u = i.texcoords.x * 2.0f / 3.0f;
                float v = i.texcoords.y;
                float s = tex2D(_ShadowMap, float2(u,v)).r;
                if (u < 1.0f / 3.0f) 
                {
                    s = min(s, tex2D(_ShadowMap, float2(u + (2.0f / 3.0f), v)).r);
                }

                // Apply depth bias.
                s += DEPTH_BIAS;

                return fixed4(s,s,s,s);
            }
            ENDCG
        }
    }
}
