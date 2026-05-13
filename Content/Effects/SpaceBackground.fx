#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 ViewportSize;
float2 CameraOffset;
float Time;
float Intensity;

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
    value += Noise(p) * 0.52;
    value += Noise(p * 2.03 + 17.3) * 0.28;
    value += Noise(p * 4.11 + 6.8) * 0.14;
    value += Noise(p * 8.07 + 41.2) * 0.06;
    return value;
}

float StarLayer(float2 uv, float scale, float threshold, float twinkle)
{
    float2 grid = uv * scale;
    float2 cell = floor(grid);
    float2 local = frac(grid) - 0.5;
    float seed = Hash(cell);
    float star = step(threshold, seed);
    float core = 1.0 - smoothstep(0.01, 0.12, length(local));
    float pulse = 0.82 + sin(seed * 54.0 + Time * twinkle) * 0.18;
    return star * core * pulse;
}

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 uv = texCoord + CameraOffset / max(ViewportSize, float2(1.0, 1.0)) * 0.08;
    float2 centered = uv * 2.0 - 1.0;
    centered.x *= ViewportSize.x / max(ViewportSize.y, 1.0);

    float vertical = saturate(uv.y);
    float radial = saturate(length(centered) * 0.62);
    float nebulaA = Fbm(uv * float2(2.0, 1.1) + float2(0.04, Time * 0.0009));
    float nebulaB = Fbm(uv * float2(4.2, 2.4) + float2(12.4, -Time * 0.0007));
    float diagonalBand = exp(-pow(centered.y + centered.x * 0.22 + 0.18, 2.0) * 2.4);
    float milkyWayAxis = centered.y + centered.x * 0.48 - 0.14;
    float milkyWayBand = exp(-milkyWayAxis * milkyWayAxis * 4.2);
    float milkyWayCore = exp(-milkyWayAxis * milkyWayAxis * 18.0);
    float milkyWayDust = Fbm(float2(centered.x * 1.1, centered.y * 3.2) + float2(23.4, Time * 0.0004));
    float milkyWayCuts = smoothstep(0.18, 0.68, milkyWayDust);
    float nebula = saturate((nebulaA * 0.72 + nebulaB * 0.28 - 0.42) * 1.45) * diagonalBand;

    float3 deepTop = float3(0.012, 0.020, 0.052);
    float3 deepBottom = float3(0.002, 0.005, 0.016);
    float3 baseColor = lerp(deepTop, deepBottom, vertical);
    float3 nebulaColor = float3(0.055, 0.075, 0.145) + float3(0.025, 0.012, 0.045) * nebulaB;
    baseColor += nebulaColor * nebula * 0.46;
    baseColor += float3(0.16, 0.18, 0.23) * milkyWayBand * milkyWayCuts * 0.22;
    baseColor += float3(0.32, 0.30, 0.26) * milkyWayCore * milkyWayDust * 0.045;

    float stars =
        StarLayer(uv, 92.0, 0.992, 0.018) * 0.42 +
        StarLayer(uv + 4.7, 47.0, 0.986, 0.012) * 0.28;
    stars += StarLayer(uv + float2(9.1, 2.7), 130.0, 0.985, 0.01) * milkyWayBand * 0.26;
    baseColor += float3(0.72, 0.82, 1.0) * stars;

    float vignette = smoothstep(1.08, 0.2, radial);
    baseColor *= lerp(0.54, 1.0, vignette);

    return float4(baseColor * Intensity, 1.0) * color;
}

technique SpaceBackground
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
