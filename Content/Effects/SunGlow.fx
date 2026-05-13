#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 ViewportSize;
float2 SunCenter;
float SunRadius;
float Intensity;
float Time;
float SurfaceNoiseScale;
float PulseSpeed;
float CoreOnly;

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

float Fbm(float2 p)
{
    float value = 0.0;
    value += Noise(p) * 0.62;
    value += Noise(p * 2.13 + 9.4) * 0.26;
    value += Noise(p * 4.01 + 31.2) * 0.12;
    return value;
}

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 pixelPosition = texCoord * ViewportSize;
    float2 relative = pixelPosition - SunCenter;

    float distanceFromSun = length(relative);
    float2 sunUv = relative / max(SunRadius, 1.0);
    float surfaceNoise = Fbm(sunUv * SurfaceNoiseScale + float2(Time * 0.012, -Time * 0.008));
    float pulse = 0.96 + sin(Time * PulseSpeed) * 0.04;

    float disk = 1.0 - smoothstep(SunRadius * 0.94, SunRadius * 1.04, distanceFromSun);
    float innerDisk = 1.0 - smoothstep(0.0, SunRadius * 0.9, distanceFromSun);
    float outerGlow = pow(saturate(1.0 - distanceFromSun / (SunRadius * 4.8)), 2.65) * pulse;
    float nearGlow = pow(saturate(1.0 - distanceFromSun / (SunRadius * 1.75)), 1.35);

    float horizontalDistance = abs(relative.y) / max(SunRadius * 0.74, 1.0);
    float horizontalWidth = abs(relative.x) / max(SunRadius * 6.2, 1.0);
    float graphicFlare = exp(-horizontalDistance * horizontalDistance * 2.1) *
        saturate(1.0 - horizontalWidth) * 0.34;

    float paperGrain = (surfaceNoise - 0.5) * 0.045;

    float glowAmount = saturate(outerGlow * 0.48 + nearGlow * 0.72 + graphicFlare + disk);
    if (CoreOnly > 0.5)
        glowAmount = disk;
    else
        glowAmount = saturate(outerGlow * 0.52 + nearGlow * 0.28 + graphicFlare * 0.72);

    glowAmount *= Intensity;

    float3 outerColor = float3(1.0, 0.38, 0.08);
    float3 middleColor = float3(1.0, 0.73, 0.16);
    float3 coreColor = float3(1.0, 0.91, 0.42);

    float3 glowColor = lerp(outerColor, middleColor, saturate(nearGlow + graphicFlare));
    glowColor = lerp(glowColor, coreColor, saturate(disk * (0.55 + innerDisk * 0.45)));
    glowColor *= 1.0 + paperGrain * disk;

    float alpha = saturate(glowAmount * (CoreOnly > 0.5 ? 1.0 : 0.62));
    return float4(glowColor * alpha, alpha) * color;
}

technique SunGlow
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
