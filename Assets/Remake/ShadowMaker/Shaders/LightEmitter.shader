Shader "ShadowMaker/LightEmitter"
{
	Properties
	{
		[PerRendererData] _ShadowTex ("Texture", 2D) = "white" {}
		[PerRendererData] _Color ("Color", Color) = (1,1,1,1)
		[PerRendererData] _LightPosition("LightPosition", Vector) = (0,0,1,0)
		[PerRendererData] _ShadowMapParams("ShadowMapParams", Vector) = (0,0,0,0)
		[PerRendererData] _Params2("Params2", Vector) = (0,0,0,0)
		[PerRendererData] _LightRadius("LightRadius", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Geometry" 
			"IgnoreProjector"="True" 
			"RenderType"="Opaque" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One One

		Pass
		{
		CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "ShadowMap1D.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float4 modelPos : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.modelPos = IN.vertex;
				OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
				return OUT;
			}

			sampler2D 	_ShadowTex;
			float4 		_LightPosition;
			float4 		_ShadowMapParams;
			float4 		_Params2;
			float4      _LightRadius;
			fixed4 		_Color;
			
			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = _Color;

				float4 lightPosition = _LightPosition;

				float2 polar = ToPolar(IN.worldPos.xy, lightPosition.xy);

				float pixelDistance = polar.y / _LightRadius.x; // [0-1]
				float u = (polar.x / 3.14f + 1.0f) * 0.5f; // [0-1] // Converts from polar angle to shadow map u-coordinate.
				float s = tex2D(_ShadowTex, float2(u, _ShadowMapParams.x)).r; // [0-LightRadius]

				float wd = (1.0f - s) * _LightRadius.x;

				return fixed4(s, s, s, 1);

				float shadow = SampleShadowTexturePCF(_ShadowTex,polar,_ShadowMapParams.x);
				if (shadow < 0.5f) {
					clip( -1.0 );
					return c;
				}
				
				float distFalloff = max(0.0f,length(IN.worldPos.xy- lightPosition.xy) - _Params2.w) * _Params2.z;
				distFalloff = clamp(distFalloff,0.0f,1.0f);
				distFalloff = pow(1.0f - distFalloff, lightPosition.z);

				float angleFalloff = AngleDiff(polar.x, _Params2.x) / _Params2.y;
				angleFalloff = clamp(angleFalloff, 0.0f, 1.0f);
				angleFalloff = pow(1.0f - angleFalloff, lightPosition.w);

				c.rgb *= distFalloff * angleFalloff;

				return c;
			}
		ENDCG
		}
	}
}
