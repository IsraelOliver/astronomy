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
float3 LightDirection;
float PlanetShadowRadius;
float PlanetShadowStrength;
float RingStyle;
float RingRotation;

sampler TextureSampler : register(s0);

float Hash(float value)
{
    return frac(sin(value * 127.1) * 43758.5453);
}

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 local = texCoord * 2.0 - 1.0;
    float cosRotation = cos(RingRotation);
    float sinRotation = sin(RingRotation);
    local = float2(
        local.x * cosRotation - local.y * sinRotation,
        local.x * sinRotation + local.y * cosRotation);

    if ((DrawFront > 0.5 && local.y < 0.0) || (DrawFront <= 0.5 && local.y > 0.0))
        discard;

    float2 ellipse = float2(local.x, local.y / max(RingTilt, 0.001));
    float radius = length(ellipse);

    float normalized = saturate((radius - InnerRadius) / max(OuterRadius - InnerRadius, 0.001));
    float innerEdge = smoothstep(InnerRadius - 0.012, InnerRadius + 0.018, radius);
    float outerEdge = 1.0 - smoothstep(OuterRadius - 0.03, OuterRadius + 0.012, radius);
    float alpha = innerEdge * outerEdge;

    float ringletAlpha =
        (1.0 - smoothstep(0.018, 0.033, abs(normalized - 0.12))) * 0.18 +
        (1.0 - smoothstep(0.012, 0.023, abs(normalized - 0.26))) * 0.24 +
        (1.0 - smoothstep(0.014, 0.026, abs(normalized - 0.39))) * 0.28 +
        (1.0 - smoothstep(0.016, 0.031, abs(normalized - 0.56))) * 0.34 +
        (1.0 - smoothstep(0.013, 0.024, abs(normalized - 0.69))) * 0.23 +
        (1.0 - smoothstep(0.026, 0.045, abs(normalized - 0.87))) * 0.72;

    alpha = RingStyle > 0.5 ? ringletAlpha * outerEdge * innerEdge : alpha;

    if (alpha <= 0.001)
        discard;

    float cassini = RingStyle > 0.5
        ? 1.0
        : 1.0 - smoothstep(0.56, 0.585, normalized) * (1.0 - smoothstep(0.625, 0.65, normalized));
    float innerGap = RingStyle > 0.5
        ? 1.0
        : 1.0 - smoothstep(0.18, 0.2, normalized) * (1.0 - smoothstep(0.225, 0.245, normalized)) * 0.42;
    float outerGap = RingStyle > 0.5
        ? 1.0
        : 1.0 - smoothstep(0.78, 0.8, normalized) * (1.0 - smoothstep(0.83, 0.85, normalized)) * 0.35;

    float bands =
        sin(normalized * 95.0) * 0.08 +
        sin(normalized * 231.0 + 1.7) * 0.035 +
        (Hash(floor(normalized * 48.0)) - 0.5) * 0.09;
    float bandLight = saturate(0.74 + bands);
    float sideFalloff = lerp(0.88, 1.06, saturate(abs(local.x)));

    float2 light2 = float2(LightDirection.x, LightDirection.z);
    if (abs(light2.x) + abs(light2.y) < 0.0001)
        light2 = float2(1.0, -0.18);
    light2 = normalize(light2);

    float2 shadowDirection = -light2;
    float alongShadow = dot(ellipse, shadowDirection);
    float sideShadow = abs(dot(ellipse, float2(-shadowDirection.y, shadowDirection.x)));
    float shadowStart = PlanetShadowRadius * 0.58;
    float shadowLength = PlanetShadowRadius * 2.45;
    float shadowWidth = PlanetShadowRadius * lerp(0.88, 0.52, saturate((alongShadow - shadowStart) / max(shadowLength, 0.001)));
    float behindPlanet = smoothstep(shadowStart, shadowStart + 0.04, alongShadow) *
        (1.0 - smoothstep(shadowStart + shadowLength, shadowStart + shadowLength + 0.12, alongShadow));
    float planetShadow = behindPlanet * (1.0 - smoothstep(shadowWidth * 0.72, shadowWidth, sideShadow));

    float3 darkColor = float3(0.39, 0.31, 0.20);
    float3 midColor = float3(0.78, 0.66, 0.43);
    float3 brightColor = float3(1.0, 0.9, 0.62);
    float3 ringColor = lerp(darkColor, midColor, normalized);
    ringColor = lerp(ringColor, brightColor, bandLight * 0.55);
    float epsilon = 1.0 - smoothstep(0.055, 0.12, abs(normalized - 0.87));
    float3 uranusDark = float3(0.055, 0.064, 0.075);
    float3 uranusMid = float3(0.23, 0.25, 0.25);
    float3 uranusEpsilon = float3(0.56, 0.50, 0.42);
    float3 uranusColor = lerp(uranusDark, uranusMid, saturate(ringletAlpha * 1.4));
    uranusColor = lerp(uranusColor, uranusEpsilon, epsilon * 0.68);
    ringColor = RingStyle > 0.5 ? uranusColor : ringColor;
    ringColor = lerp(ringColor, darkColor * 0.58, planetShadow * PlanetShadowStrength);

    alpha *= cassini * innerGap * outerGap * MaxAlpha * sideFalloff;
    return float4(ringColor * alpha, saturate(alpha)) * color;
}

technique SaturnRings
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
