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
        Cull Off
		ZWrite Off
		ZTest Off
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

			float Intersect(float2 lineOneStart, float2 lineOneEnd, float2 lineTwoStart, float2 lineTwoEnd)
			{
				float2 line2Perp = float2(lineTwoEnd.y - lineTwoStart.y, lineTwoStart.x - lineTwoEnd.x);
				float line1Proj = dot(lineOneEnd - lineOneStart, line2Perp);

				if (abs(line1Proj) < 1e-4)
					return 0.0f;

				float t1 = dot(lineTwoStart - lineOneStart, line2Perp) / line1Proj;
				return t1;
			}

			struct appdata
			{
				float3 vertex1 : POSITION;
				float2 vertex2 : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 edge   : TEXCOORD0;		// xy=edgeVertex1,yz=edgeVertex2
				float2 polar  : TEXCOORD1;		// x=angle,y=distance
			};

			v2f vert(appdata v)
			{
				float2 lightPosition = _LightPosition.xy;

				float2 polar1 = ToPolar(v.vertex1.xy, lightPosition);
				float2 polar2 = ToPolar(v.vertex2.xy, lightPosition);

				float angle1 = polar1.x;
				float angle2 = polar2.x;

				v2f o;
				o.edge = float4(v.vertex1.xy, v.vertex2.xy);
				o.edge = lerp(o.edge, o.edge.zwxy, step(angle1, angle2));

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
				float2 lightPosition = _LightPosition.xy;
				float lightRadius = _LightRadius.x;
				
				float angle = i.polar.x;
				float dist = i.polar.y;

				float d = dist / lightRadius;
				//return float4(d, d, d, d);

				if (AngleDiff(angle,_LightPosition.z) > _LightPosition.w)
					return float4(0,0,0,0);

				float2 realEnd = lightPosition + float2(cos(angle) * 10, sin(angle) * 10);

				float t = Intersect(lightPosition, realEnd, i.edge.xy, i.edge.zw);

				return float4(t,t,t,t);
			}
			ENDCG
        }
    }
}
