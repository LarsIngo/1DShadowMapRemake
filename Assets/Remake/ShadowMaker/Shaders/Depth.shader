Shader "ShadowMaker/Depth"
{
    SubShader
    {
        Tags 
        {
            "RenderType" = "Opaque"
        }
        Pass 
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 depth : TEXCOORD0;
                //float dist : TEXCOORD1;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_DEPTH(o.depth);
                //o.dist = mul(UNITY_MATRIX_IT_MV, v.vertex).z; // https://forum.unity.com/threads/render-depth-distance.272979/
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_OUTPUT_DEPTH(i.depth);
                //return fixed4(i.dist, i.dist, i.dist, 1);
            }

            ENDCG
        }
    }
}
