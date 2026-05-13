#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 ViewportSize;
float2 SunCenter;
float SunRadius;
float Time;
float Intensity;
float OrbitalPlaneOnly;

sampler TextureSampler : register(s0);

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = Hash(i);
    float b = Hash(i + float2(1.0, 0.0));
    float c = Hash(i + float2(0.0, 1.0));
    float d = Hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 pixelPosition = texCoord * ViewportSize;
    float2 relative = pixelPosition - SunCenter;
    float distanceFromSun = length(relative);

    float2 direction = relative / max(distanceFromSun, 1.0);
    float radial = saturate((distanceFromSun - SunRadius * 1.4) / max(SunRadius * 8.8, 1.0));
    float falloff = pow(saturate(1.0 - radial), 2.15);
    float innerCut = smoothstep(SunRadius * 1.15, SunRadius * 1.9, distanceFromSun);

    float angle = atan2(direction.y, direction.x);
    float rays =
        sin(angle * 11.0 + Time * 0.018) * 0.5 +
        sin(angle * 23.0 - Time * 0.011) * 0.32 +
        sin(angle * 37.0 + Time * 0.007) * 0.18;
    rays = smoothstep(0.42, 0.92, rays * 0.5 + 0.5);

    float dustNoise = Noise(direction * 12.0 + radial * 18.0 + float2(Time * 0.012, -Time * 0.006));
    float fineDust = smoothstep(0.68, 0.96, dustNoise);

    float horizontalPlane = exp(-relative.y * relative.y / max(SunRadius * SunRadius * 2.4, 1.0));
    float planeDust = horizontalPlane * smoothstep(0.08, 0.52, radial) * (1.0 - smoothstep(0.76, 1.0, radial));

    float planeDistance = abs(relative.y) / max(SunRadius * 2.2, 1.0);
    float broadPlane = exp(-planeDistance * planeDistance) *
        smoothstep(SunRadius * 2.2, SunRadius * 5.8, distanceFromSun) *
        (1.0 - smoothstep(SunRadius * 22.0, SunRadius * 36.0, distanceFromSun));
    float planeTexture = lerp(0.68, 1.16, Noise(float2(relative.x * 0.006, relative.y * 0.035) + Time * 0.001));

    float alpha = OrbitalPlaneOnly > 0.5
        ? broadPlane * planeTexture * Intensity * 0.18
        : (rays * 0.34 + fineDust * 0.24 + planeDust * 0.32) * falloff * innerCut * Intensity * 0.42;
    alpha = saturate(alpha);

    float3 warm = float3(1.0, 0.62, 0.2);
    float3 pale = float3(1.0, 0.86, 0.46);
    float3 planeColor = float3(0.58, 0.66, 0.86);
    float3 dustColor = OrbitalPlaneOnly > 0.5
        ? lerp(warm * 0.42, planeColor, saturate(distanceFromSun / max(SunRadius * 28.0, 1.0)))
        : lerp(warm, pale, saturate(radial * 0.8 + fineDust * 0.2));

    return float4(dustColor * alpha, alpha) * color;
}

technique SolarDust
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
