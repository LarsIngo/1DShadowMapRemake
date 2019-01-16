Shader "ShadowMaker/ShadowMapFinalBlur"
{
    Properties
    {
        [PerRendererData] _ShadowMap ("Texture", 2D) = "white" {}
        [PerRendererData] _Params("Params", Vector) = (0,0,0,0)
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

            #define WEIGHT0 0.2270270270f
            #define WEIGHT1 0.1945945946f
            #define WEIGHT2 0.1216216216f
            #define WEIGHT3 0.0540540541f
            #define WEIGHT4 0.0162162162f
            
            struct appdata_t
            {
                float4 vertex    : POSITION;
                float2 texcoords : TEXCOORD0; 
            };

            appdata_t vert(appdata_t IN)
            {
                return IN;
            }

            sampler2D _ShadowMap;
            float4 _Params;

            float4 frag(appdata_t IN) : SV_Target
            {
                float2 uv = IN.texcoords;

                // Fragment width in UV-space. Offset scales with depth.
                float dU = _Params.x;// *smoothstep(0.0f, 1.0f, center);

                // Horizontal blur.
                float value = 0.0f;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * -4.0f), uv.y)).r * WEIGHT4;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * -3.0f), uv.y)).r * WEIGHT3;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * -2.0f), uv.y)).r * WEIGHT2;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * -1.0f), uv.y)).r * WEIGHT1;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * +0.0f), uv.y)).r * WEIGHT0;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * +1.0f), uv.y)).r * WEIGHT1;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * +2.0f), uv.y)).r * WEIGHT2;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * +3.0f), uv.y)).r * WEIGHT3;
                value += tex2D(_ShadowMap, float2(uv.x + (dU * +4.0f), uv.y)).r * WEIGHT4;

                return float4(value, value, value, value);
            }
            ENDCG
        }
    }
}
