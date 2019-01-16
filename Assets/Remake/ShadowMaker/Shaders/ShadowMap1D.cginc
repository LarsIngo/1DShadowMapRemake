#define WEIGHT0 0.2270270270f
#define WEIGHT1 0.1945945946f
#define WEIGHT2 0.1216216216f
#define WEIGHT3 0.0540540541f
#define WEIGHT4 0.0162162162f

// General functions for 1D shadow mapping
//
//
inline float2 ToPolar(float2 cartesian, float2 center)
{
    float2 d = cartesian - center;
    return float2(atan2(d.y, d.x), length(d));
}

// convert from (-PI to +2PI) to (-1,+1)
// the 3PI range is the normal 2PI plus another PI to deal
// with wrap around e.g if a span goes from say 350 to 10 degrees
// (20 degrees shortest path) it would require splitting the span
// into 2 parts, 350-360 and 0-10, which is not possible in a vertex
// shader (maybe a geometry shader would be fine). Instead we make the
// span go from 350-370 and then when sampling from 0-PI you must
// also sample from 2PI to 3PI and take the min to resolve the
// wraparound.
inline float PolarAngleToClipSpace(float a)
{
    a += UNITY_PI;
    a *= 2.0f/(UNITY_PI*3.0f);
    a -= 1.0f;			
    return a;
}
// convert from (-PI to +PI) to (0,2/3)
// The final (1/3) is the wraparound as discussed above.
// if the returned angle is < 1/3 you should sample
// again with 2/3 added on and take the min.
inline float PolarAngleToU(float angle)
{
	return (angle + UNITY_PI) / (2.0f * UNITY_PI);
}

// Takes a single sample from the shadow texture.
inline float SampleShadowMap(sampler2D textureSampler, float u, float v)
{
    return tex2D(textureSampler, float2(u, v)).r;
}

inline float SampleShadowTexturePCF3(sampler2D textureSampler, float2 polar, float v, float shadowMapResolution)
{
	float u1 = PolarAngleToU(polar.x);

	float dU = 1.0f / shadowMapResolution;

	float u2 = u1 - 1.0f * dU;
	float u3 = u1 + 1.0f * dU;

	float total = 0.0f;
	total += step(polar.y, SampleShadowMap(textureSampler, u1, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u2, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u3, v) * 10.0f);

	return total / 3.0f;
}

inline float SampleShadowTexturePCF5(sampler2D textureSampler, float2 polar, float v, float shadowMapResolution)
{
	float u1 = PolarAngleToU(polar.x);

	float dU = 1.0f / shadowMapResolution;

	float u2 = u1 - 2.0f * dU;
	float u3 = u1 - 1.0f * dU;
	float u4 = u1 + 1.0f * dU;
	float u5 = u1 + 2.0f * dU;

	float total = 0.0f;
	total += step(polar.y, SampleShadowMap(textureSampler, u1, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u2, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u3, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u4, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u5, v) * 10.0f);

	return total / 5.0f;
}

inline float SampleShadowTexturePCF9(sampler2D textureSampler, float2 polar, float v, float shadowMapResolution)
{
	float u1 = PolarAngleToU(polar.x);

	float dU = 1.0f / shadowMapResolution;

	float u2 = u1 - 4.0f * dU;
	float u3 = u1 - 3.0f * dU;
	float u4 = u1 - 2.0f * dU;
	float u5 = u1 - 1.0f * dU;
	float u6 = u1 + 1.0f * dU;
	float u7 = u1 + 2.0f * dU;
	float u8 = u1 + 3.0f * dU;
	float u9 = u1 + 4.0f * dU;

	float total = 0.0f;
	total += step(polar.y, SampleShadowMap(textureSampler, u1, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u2, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u3, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u4, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u5, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u6, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u7, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u8, v) * 10.0f);
	total += step(polar.y, SampleShadowMap(textureSampler, u9, v) * 10.0f);

	return total / 9.0f;
}

inline float SampleShadowTexturePCF9G(sampler2D textureSampler, float2 polar, float v, float shadowMapResolution)
{
	float u1 = PolarAngleToU(polar.x);

	float dU = 1.0f / shadowMapResolution;

	float u2 = u1 - 4.0f * dU;
	float u3 = u1 - 3.0f * dU;
	float u4 = u1 - 2.0f * dU;
	float u5 = u1 - 1.0f * dU;
	float u6 = u1 + 1.0f * dU;
	float u7 = u1 + 2.0f * dU;
	float u8 = u1 + 3.0f * dU;
	float u9 = u1 + 4.0f * dU;

	float total = 0.0f;
	total += step(polar.y, SampleShadowMap(textureSampler, u1, v) * 10.0f) * WEIGHT0;
	total += step(polar.y, SampleShadowMap(textureSampler, u2, v) * 10.0f) * WEIGHT4;
	total += step(polar.y, SampleShadowMap(textureSampler, u3, v) * 10.0f) * WEIGHT3;
	total += step(polar.y, SampleShadowMap(textureSampler, u4, v) * 10.0f) * WEIGHT2;
	total += step(polar.y, SampleShadowMap(textureSampler, u5, v) * 10.0f) * WEIGHT1;
	total += step(polar.y, SampleShadowMap(textureSampler, u6, v) * 10.0f) * WEIGHT1;
	total += step(polar.y, SampleShadowMap(textureSampler, u7, v) * 10.0f) * WEIGHT2;
	total += step(polar.y, SampleShadowMap(textureSampler, u8, v) * 10.0f) * WEIGHT3;
	total += step(polar.y, SampleShadowMap(textureSampler, u9, v) * 10.0f) * WEIGHT4;

	return total;
}

// Returns the shortest angle arc between a and b (all angles in radians)
inline float AngleDiff(float a, float b)
{
    float diff = fmod(abs(a-b), 2.0f * UNITY_PI);
    if (diff > UNITY_PI)
		diff = 2.0f * UNITY_PI - diff;

    return diff;
}

inline float2 ClipSpaceToUV(float2 clipSpace)
{
    #if UNITY_UV_STARTS_AT_TOP
    float4 scale = float4(0.5f, 0.5f, 0.5f, 0.5f);
    #else
    float4 scale = float4(0.5f, -0.5f, 0.5f, 0.5f);
    #endif

    return clipSpace * scale.xy + scale.zw;
}
