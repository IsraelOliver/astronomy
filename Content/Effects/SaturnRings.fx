#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float RingTilt;
float InnerRadius;
float OuterRadius;
float DrawFront;
float MaxAlpha;

sampler TextureSampler : register(s0);

float Hash(float value)
{
    return frac(sin(value * 127.1) * 43758.5453);
}

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 local = texCoord * 2.0 - 1.0;

    if ((DrawFront > 0.5 && local.y < 0.0) || (DrawFront <= 0.5 && local.y > 0.0))
        discard;

    float2 ellipse = float2(local.x, local.y / max(RingTilt, 0.001));
    float radius = length(ellipse);

    float innerEdge = smoothstep(InnerRadius - 0.012, InnerRadius + 0.018, radius);
    float outerEdge = 1.0 - smoothstep(OuterRadius - 0.03, OuterRadius + 0.012, radius);
    float alpha = innerEdge * outerEdge;

    if (alpha <= 0.001)
        discard;

    float normalized = saturate((radius - InnerRadius) / max(OuterRadius - InnerRadius, 0.001));
    float cassini = 1.0 - smoothstep(0.56, 0.585, normalized) * (1.0 - smoothstep(0.625, 0.65, normalized));
    float innerGap = 1.0 - smoothstep(0.18, 0.2, normalized) * (1.0 - smoothstep(0.225, 0.245, normalized)) * 0.42;
    float outerGap = 1.0 - smoothstep(0.78, 0.8, normalized) * (1.0 - smoothstep(0.83, 0.85, normalized)) * 0.35;

    float bands =
        sin(normalized * 95.0) * 0.08 +
        sin(normalized * 231.0 + 1.7) * 0.035 +
        (Hash(floor(normalized * 48.0)) - 0.5) * 0.09;
    float bandLight = saturate(0.74 + bands);
    float frontLight = DrawFront > 0.5 ? 1.12 : 0.72;
    float sideFalloff = lerp(0.82, 1.08, saturate(abs(local.x)));

    float3 darkColor = float3(0.39, 0.31, 0.20);
    float3 midColor = float3(0.78, 0.66, 0.43);
    float3 brightColor = float3(1.0, 0.9, 0.62);
    float3 ringColor = lerp(darkColor, midColor, normalized);
    ringColor = lerp(ringColor, brightColor, bandLight * 0.55);

    alpha *= cassini * innerGap * outerGap * MaxAlpha * frontLight * sideFalloff;
    return float4(ringColor * alpha, saturate(alpha)) * color;
}

technique SaturnRings
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
