Shader "ShadowMaker/ShadowMapInitial"
{
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

			#define EMITTER_COUNT_MAX 64

            float4 _EmitterParams;			// xy is the position, z is the angle in radians, w is the radius of the light.
            float4 _ShadowMapParams;		// this is the row to write to in the shadow map. x is used to write, y to read.

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
				float2 angle  : TEXCOORD1;		// x=angle,y=none
			};

			v2f vert(appdata v)
			{
				// Chache global memory.
				float2 lightPosition = _EmitterParams.xy;

				// Convert vertices to polar coordinates.
				float2 polar1 = ToPolar(v.vertex1.xy, lightPosition);
				float2 polar2 = ToPolar(v.vertex2.xy, lightPosition);

				// Get angles.
				float angle1 = polar1.x;
				float angle2 = polar2.x;

				v2f o;

				// Store edge.
				o.edge = float4(v.vertex1.xy, v.vertex2.xy);
				o.edge = lerp(o.edge, o.edge.zwxy, step(angle1, angle2));

				// Check whether vertex edge was cut off.
				float diff = abs(angle1 - angle2);
				if (diff >= UNITY_PI)
				{
					// Sign is used to reduce branching.
					float factor = (sign(angle1 - angle2) + 1.0f) * 0.5f;
					angle1 = max(angle1, angle2) + (2.0f * UNITY_PI - diff) * factor;
				}
				//float diff = abs(angle1 - angle2);
				//if (diff >= UNITY_PI)
				//{
				//	float maxAngle = max(angle1, angle2);
				//	if (angle1 == maxAngle)
				//	{
				//		angle1 = maxAngle + 2.0f * UNITY_PI - diff;
				//	}
				//	else
				//	{
				//		angle1 = maxAngle;
				//	}
				//}

				// Convert vertex to clip space.
				o.vertex = float4(PolarAngleToClipSpace(angle1), _ShadowMapParams.y, 0.0f, 1.0f);

				// Store angle.
				o.angle = float2(angle1, 0.0f);

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// Chache global memory.
				float4 emitterParams = _EmitterParams;
				float2 lightPosition = emitterParams.xy;
				float radius = emitterParams.w;
				
				// Get angle.
				float angle = i.angle.x;

				// Check whether angle is outside spread of light.
				//if (AngleDiff(angle, lightPosition.z) > lightPosition.w)
				//	return float4(0,0,0,0);

				// Calculate postion of the light on the edge.
				float2 lightEnd = lightPosition + float2(cos(angle) * radius, sin(angle) * radius);

				// Find intersection between light vector and vertex edge.
				float t = Intersect(lightPosition, lightEnd, i.edge.xy, i.edge.zw);

				// Return intersection scalar value.
				return float4(t,t,t,t);
			}
			ENDCG
        }
    }
}
