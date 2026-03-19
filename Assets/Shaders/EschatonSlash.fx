sampler uImage0 : register(s0);

float uTime;
float4 uColor;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.position = mul(input.position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    const float pi = 3.14159265358979;
    
    float xFade = smoothstep(0.0, 0.2, coords.x) * (1.0 - coords.x);
    xFade = saturate(xFade);
    
    float crestLine = 1.0 - ((0.1 + 0.06) * pow(xFade, 2.0));
    float crestMask = step(crestLine, coords.y);
    
    float2 uv1 = float2(coords.x * 1.2 - uTime * 0.95, coords.y * 1.6 + uTime * 0.08);
    float2 uv2 = float2(coords.x * 2.4 + uTime * 0.25, coords.y * 2.8 - uTime * 0.05);

    float n1 = dot(tex2D(uImage0, uv1).rgb, float3(0.3333, 0.3333, 0.3333));
    float n2 = dot(tex2D(uImage0, uv2).rgb, float3(0.3333, 0.3333, 0.3333));
    float noiseValue = saturate(lerp(n1, n2, 0.5));
    
    float bodyMask = pow(saturate(coords.y * xFade), 1.15);
    
    float causticWave = sin((coords.x * 10.0 - coords.y * 4.0 - uTime * 3.2) * pi);
    float caustic = smoothstep(0.2, 1.0, causticWave * 0.5 + noiseValue);
    caustic *= bodyMask * xFade;

    float depthLerp = saturate(coords.y);
    float3 deepWater = float3(0.02, 0.10, 0.20);
    float3 midWater = float3(0.00, 0.42, 0.55);
    float3 shallowWater = float3(0.65, 0.95, 1.00);

    float3 waterColor = lerp(deepWater, midWater, smoothstep(0.0, 0.65, depthLerp));
    waterColor = lerp(waterColor, shallowWater, smoothstep(0.55, 1.0, depthLerp) * 0.45);
    
    float foamNoise = smoothstep(0.45, 0.95, noiseValue);
    float foam = crestMask * foamNoise;
    float innerGlow = 0.35 + caustic * 0.9;
    float3 bodyCol = waterColor * bodyMask * innerGlow;
    float distort = (noiseValue - 0.5) * 2.0;

    float ribWaveA = sin((coords.x * 14.0 - coords.y * 5.0 - uTime * 1.8 + distort) * 3.14159265);
    float ribWaveB = sin((coords.x * 27.0 + coords.y * 8.0 + uTime * 0.9 - distort * 1.6) * 3.14159265);

    float ribMask = smoothstep(0.45, 0.95, ribWaveA * 0.6 + ribWaveB * 0.3 + noiseValue * 0.8);
    ribMask *= bodyMask * (1.0 - coords.y) * xFade;

    float membraneShadow = smoothstep(0.3, 0.9, ribMask) * 0.35;
    float3 membraneColor = float3(0.08, 0.16, 0.22);

    bodyCol = lerp(bodyCol, membraneColor, membraneShadow);
    bodyCol += float3(0.25, 0.10, 0.35) * ribMask * 0.18;
    float crestGlow = smoothstep(0.9, 1.0, coords.y) * xFade;
    bodyCol += shallowWater * crestGlow * 0.85;

    // Foam edge color.
    float3 foamCol = lerp(float3(0.75, 0.95, 1.0), float3(1.0, 1.0, 1.0), foamNoise);

    float3 finalRgb = bodyCol + foamCol * foam * 0.95;
    
    finalRgb *= lerp(float3(1.0, 1.0, 1.0), input.Color.rgb, 0.35) + bodyCol;
    
    bool edge = coords.y > 1 - ((0.3f) * pow(1-coords.x, 4));
    float alpha = saturate(bodyMask * 0.95 + foam * 0.6);

    return foam+ float4(finalRgb, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}