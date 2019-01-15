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
				float dU = _Params.x;

				float s = 0.0f;
				for (int kx = -HK; kx <= HK; ++kx)
				{
					s += tex2D(_ShadowMap, IN.texcoords + float2(dU * kx, 0.0f)).r;
				}

				s = s / KERNEL_SIZE;

                return fixed4(s,s,s,s);
            }
			ENDCG
        }
    }
}
