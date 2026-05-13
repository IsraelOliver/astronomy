#if OPENGL
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 ViewportSize;
float2 MaskCenter;
float MaskRadius;
float Feather;

sampler TextureSampler : register(s0);

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(TextureSampler, texCoord) * color;
    float2 pixelPosition = texCoord * ViewportSize;
    float distanceFromMask = distance(pixelPosition, MaskCenter);
    float visibility = smoothstep(MaskRadius - Feather, MaskRadius + Feather, distanceFromMask);

    return source * visibility;
}

technique SoftCircleMask
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
