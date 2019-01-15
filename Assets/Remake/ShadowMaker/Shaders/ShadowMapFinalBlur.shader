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

			#define KERNEL_SIZE 9
			#define HK (KERNEL_SIZE - 1) / 2
            
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

            fixed4 frag(appdata_t IN) : SV_Target
            {
				return tex2D(_ShadowMap, IN.texcoords);
			    // TMP

				//float2 uv = IN.texcoords;

				//// The depth of current fragment.
				//float center = tex2D(_ShadowMap, uv).r;

				//// Fragment width in UV-space. Offset scales with depth.
				//float dU = _Params.x * smoothstep(0.0f, 1.0f, center);

				//// Gussian blur.
				//float s = 0.0f;

				//s += tex2D(_ShadowMap, float2(uv.x - (4.0f * dU), uv.y)).r * 0.05f;
				//s += tex2D(_ShadowMap, float2(uv.x - (3.0f * dU), uv.y)).r * 0.09f;
				//s += tex2D(_ShadowMap, float2(uv.x - (2.0f * dU), uv.y)).r * 0.12f;
				//s += tex2D(_ShadowMap, float2(uv.x - (1.0f * dU), uv.y)).r * 0.15f;

				//s += center * 0.16f;

				//s += tex2D(_ShadowMap, float2(uv.x + (1.0f * dU), uv.y)).r * 0.15f;
				//s += tex2D(_ShadowMap, float2(uv.x + (2.0f * dU), uv.y)).r * 0.12f;
				//s += tex2D(_ShadowMap, float2(uv.x + (3.0f * dU), uv.y)).r * 0.09f;
				//s += tex2D(_ShadowMap, float2(uv.x + (4.0f * dU), uv.y)).r * 0.05f;

    //            return fixed4(s,s,s,s);
            }
			ENDCG
        }
    }
}
