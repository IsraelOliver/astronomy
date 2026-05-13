#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4 LightColor;
float4 BaseColor;
float4 ShadowColor;
float4 AtmosphereColor;
float4 OutlineColor;
float3 LightDirection;
float PlanetRadius;
float AtmosphereIntensity;
float LightThreshold;
float ShadowThreshold;
float BandSoftness;
float BandEdge0;
float BandEdge1;
float BandEdge2;
float BandEdge3;
float TextureOverlayStrength;
float OutlineStrength;
float NoiseScale;
float Time;

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
    float2 local = texCoord * 2.0 - 1.0;
    float distanceFromCenter = length(local);

    if (distanceFromCenter > 1.0)
        discard;

    float planetEdge = max(PlanetRadius, 0.001);
    float alpha = 0.0;
    float3 result = float3(0.0, 0.0, 0.0);

    float3 light3 = LightDirection;
    if (abs(light3.x) + abs(light3.y) + abs(light3.z) < 0.0001)
        light3 = float3(1.0, 0.0, 0.2);
    light3 = normalize(light3);

    float2 light2 = normalize(float2(light3.x, light3.z));

    if (distanceFromCenter <= planetEdge)
    {
        float2 planetUv = local / planetEdge;

        // Volume first: this reconstructs a soft sphere only to place light and shadow correctly.
        float sphereDistance = saturate(dot(planetUv, planetUv));
        float3 normal = normalize(float3(planetUv, sqrt(max(1.0 - sphereDistance, 0.0))));
        float lightAxis = dot(normal, light3);

        // Drawing layer: shadow and light are large masses; base color is a narrow painted terminator.
        float lightValue = saturate(lightAxis * 0.5 + 0.5);
        float edge0 = smoothstep(BandEdge0 - BandSoftness, BandEdge0 + BandSoftness, lightValue);
        float edge1 = smoothstep(BandEdge1 - BandSoftness, BandEdge1 + BandSoftness, lightValue);
        float shadowBand = 1.0 - edge0;
        float lightBand = edge1;
        float baseBand = saturate(edge0 - edge1);
        float litTextureMask = smoothstep(BandEdge1, BandEdge3, lightValue);
        float paintMask = litTextureMask;

        result = ShadowColor.rgb;
        result = lerp(result, BaseColor.rgb, edge0);
        result = lerp(result, LightColor.rgb, edge1);
        result = lerp(result, BaseColor.rgb, baseBand * 0.9);

        float litHemisphere = smoothstep(BandEdge0, BandEdge3, lightValue);
        float formLight = lerp(1.0, 1.05, litHemisphere);
        result *= formLight;

        // Graphic paint pass. It sits on top of the volume and reads as 2D drawing, not as relief.
        float paintNoise = Noise(planetUv * NoiseScale + float2(Time * 0.006, -Time * 0.004));
        float broadNoise = Noise(planetUv * (NoiseScale * 0.34) + 12.7);
        float2 strokeDirection = normalize(float2(-light2.y, light2.x));
        float strokeWave = sin((dot(planetUv, strokeDirection) * 13.0 + broadNoise * 1.7) * 3.14159);
        float strokes = smoothstep(0.72, 0.96, strokeWave * 0.5 + 0.5);
        strokes *= lerp(0.08, 1.0, paintMask);

        float terminatorLine = baseBand;
        float highlightPatch = lightBand * smoothstep(0.2, 0.88, paintNoise) * (1.0 - smoothstep(0.58, 0.96, distanceFromCenter / planetEdge));

        result = lerp(result, result * 0.82, strokes * TextureOverlayStrength);
        result = lerp(result, BaseColor.rgb, terminatorLine * TextureOverlayStrength * 0.75);
        result = lerp(result, LightColor.rgb, highlightPatch * TextureOverlayStrength * 0.8);

        float paperGrain = paintNoise - 0.5;
        result *= 1.0 + paperGrain * TextureOverlayStrength * (0.08 + paintMask * 0.34);

        float outline = smoothstep(planetEdge * 0.84, planetEdge, distanceFromCenter);
        result = lerp(result, OutlineColor.rgb, outline * OutlineStrength);

        alpha = 1.0 - smoothstep(planetEdge * 0.985, planetEdge, distanceFromCenter) * 0.05;
    }
    else
    {
        float haloPosition = saturate((distanceFromCenter - planetEdge) / max(1.0 - planetEdge, 0.001));
        float halo = pow(saturate(1.0 - haloPosition), 2.8);
        float sunSide = saturate(dot(normalize(local), light2) * 0.5 + 0.5);

        alpha = halo * AtmosphereIntensity * lerp(0.04, 0.38, sunSide);
        result = AtmosphereColor.rgb * lerp(0.62, 1.12, sunSide);
    }

    alpha = saturate(alpha) * color.a;
    return float4(saturate(result) * alpha, alpha);
}

technique ToonPlanet
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
