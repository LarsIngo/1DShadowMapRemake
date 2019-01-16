Shader "ShadowMaker/ShadowMapInitial"
{
    Properties
    {
        [PerRendererData] _LightPosition("LightPosition", Vector) = (0,0,0,0)
        [PerRendererData] _ShadowMapV("ShadowMapParams", Vector) = (0,0,0,0)
        [PerRendererData] _LightRadius("LightRadius", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Off
        Blend One One
        BlendOp Min

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "ShadowMap1D.cginc"

            float4 _LightPosition;			// xy is the position, z is the angle in radians, w is half the viewcone in radians
            float4 _ShadowMapParams;		// this is the row to write to in the shadow map. x is used to write, y to read.
            float4 _LightRadius;			// x is the radius of the light.

            struct appdata
            {
                float3 vertex1 : POSITION;
                float2 vertex2 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 edge   : TEXCOORD0;		// xy=edgeVertex1,yz=edgeVertex2
                float2 polar  : TEXCOORD1;		// x=angle,y=length
            };

            v2f vert(appdata v)
            {
                float2 lightPosition = _LightPosition.xy; // world position

                float2 polar1 = ToPolar(v.vertex1.xy, lightPosition);
                float2 polar2 = ToPolar(v.vertex2.xy, lightPosition);

                float angle1 = polar1.x;
                float angle2 = polar2.x;

                v2f o;
                o.edge = float4(v.vertex1.xy,v.vertex2.xy);
                o.edge = lerp(o.edge,o.edge.zwxy,step(angle1, angle2));

                float diff = abs(angle1 - angle2);
                if (diff >= UNITY_PI)
                {
                    float maxAngle = max(angle1, angle2);
                    if (angle1 == maxAngle)
                    {
                        angle1 = maxAngle + 2.0f * UNITY_PI - diff;
                    }
                    else
                    {
                        angle1 = maxAngle;
                    }
                }

                o.vertex = float4(PolarAngleToClipSpace(angle1), _ShadowMapParams.y, 0.0f, 1.0f);

                o.polar = float2(angle1, polar1.y);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float dist = clamp(i.polar.y / _LightRadius.x, 0.0f, 1.0f);
                return float4(dist, dist, dist, dist);
            }
            ENDCG
        }
    }
}
