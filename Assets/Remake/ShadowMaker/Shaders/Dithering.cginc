inline float DitherRand(float2 co)
{
    float a = 12.9898f;
    float b = 78.233f;
    float c = 43758.5453f;
    float dt = dot(co.xy, float2(a, b));
    float sn = fmod(dt, UNITY_PI);
    return frac(sin(sn) * c);
}

inline float DitherValue(float2 uv)
{
    return DitherRand(uv + float2(_Time.y, 0.0f)) / 255.0f;
}
