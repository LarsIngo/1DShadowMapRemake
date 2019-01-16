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
        Blend SrcAlpha One // Additive blending with Alpha.
        //Blend One One // Additive blending without Alpha.
        //Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency
        //Blend One OneMinusSrcAlpha // Premultiplied transparency
        //Blend OneMinusDstColor One // Soft Additive
        //Blend DstColor Zero // Multiplicative
        //Blend DstColor SrcColor // 2x Multiplicative

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
            
            v2f vert(appdata_t i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.texcoords = i.texcoords;
                o.modelPos = i.vertex;
                o.worldPos = mul(unity_ObjectToWorld, i.vertex);
                return o;
            }

            sampler2D 	_ShadowMap;
            sampler2D 	_ShadowMapBlurred;
            float4 		_LightPosition;
            float4 		_ShadowMapParams;
            float4 		_Params2;
            float4      _LightRadius;
            fixed4 		_Color;
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Cache global memory.
                fixed4 c = _Color;
                float4 lightPosition = _LightPosition;
                float4 params2 = _Params2;
                float u = _ShadowMapParams.x;
                float shadowMapResolution = 1024.0f;
				float lightRadius = _LightRadius.x;

				// Calculate polar coordinate.
                float2 polar = ToPolar(i.worldPos.xy, lightPosition.xy);

                // Caclulate shadow factor.
                float shadowFactor = SampleShadowTexturePCF3(_ShadowMap, polar, u, shadowMapResolution, lightRadius);
                
                // Calculate distance fall off.
                float distFalloff = max(0.0f, length(i.worldPos.xy - lightPosition.xy) - params2.w) * params2.z;
                distFalloff = clamp(distFalloff, 0.0f, 1.0f);
                distFalloff = pow(1.0f - distFalloff, lightPosition.z);

                // Calculate angle fall off.
                float angleFalloff = AngleDiff(polar.x, params2.x) / params2.y;
                angleFalloff = clamp(angleFalloff, 0.0f, 1.0f);
                angleFalloff = pow(1.0f - angleFalloff, lightPosition.w);

                // Calculate color.
                c.rgb *= distFalloff * angleFalloff * shadowFactor;

                // Apply dithering in order to reduce banding.
                float dither = DitherValue(i.texcoords);
                c += float4(dither, dither, dither, dither);

                return c;
            }

        ENDCG
        }
    }
}
