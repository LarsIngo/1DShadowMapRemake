Shader "ShadowMaker/LightEmitter"
{
	Properties
	{
		[PerRendererData] _ShadowMap ("Texture", 2D) = "white" {}
		[PerRendererData] _ShadowMapBlurred ("Texture", 2D) = "white" {}
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
			#include "Dithering.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoords : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoords : TEXCOORD0;
				float4 modelPos : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoords = IN.texcoords;
				OUT.modelPos = IN.vertex;
				OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
				return OUT;
			}

			sampler2D 	_ShadowMap;
			sampler2D 	_ShadowMapBlurred;
			float4 		_LightPosition;
			float4 		_ShadowMapParams;
			float4 		_Params2;
			float4      _LightRadius;
			fixed4 		_Color;
			
			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = _Color;

				float4 lightPosition = _LightPosition;
				float shadowMapParams = _ShadowMapParams.x;
				float4 params2 = _Params2;
				float lightRadius = _LightRadius.x;

				// Angle and distance.
				float2 polar = ToPolar(IN.worldPos.xy, lightPosition.xy);

				// Calculate shadow map occulution.
				float pixelDistance = polar.y / lightRadius; // Covert from world to light radius space.
				float u = (polar.x / UNITY_PI + 1.0f) * 0.5f; // [0-1] // Converts from polar angle to shadow map u-coordinate.
				float shadowMapDistance = tex2D(_ShadowMap, float2(u, shadowMapParams)).r; // [0-1]
				float shadowMapBlurredDistance = tex2D(_ShadowMapBlurred, float2(u, shadowMapParams)).r; // [0-1] // TODO use this to make soft edges.
				float shadowMapFactor = pixelDistance < shadowMapDistance ? 1.0f : 0.0f; // Whether pixel is not in shadow.
				
				// Calculate distance fall off.
				float distFalloff = max(0.0f, length(IN.worldPos.xy - lightPosition.xy) - params2.w) * params2.z;
				distFalloff = clamp(distFalloff, 0.0f, 1.0f);
				distFalloff = pow(1.0f - distFalloff, lightPosition.z);

				// Calculate angle fall off.
				float angleFalloff = AngleDiff(polar.x, params2.x) / params2.y;
				angleFalloff = clamp(angleFalloff, 0.0f, 1.0f);
				angleFalloff = pow(1.0f - angleFalloff, lightPosition.w);

				// Calculate color.
				c.rgb *= distFalloff * angleFalloff * shadowMapFactor;

				// Apply dithering in order to reduce banding.
				float dither = DitherValue(IN.texcoords) * 2.0f;
				c += float4(dither, dither, dither, dither);

				return c;
			}
		ENDCG
		}
	}
}
