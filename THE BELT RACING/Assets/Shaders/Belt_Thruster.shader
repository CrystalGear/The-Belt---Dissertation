
Shader "TheBelt/Thruster Plasma URP"
{
    Properties
    {
        [HDR] _CoreColor ("Core Colour", Color) = (2, 3, 4, 1)
        [HDR] _OuterColor ("Outer Colour", Color) = (0.05, 0.4, 2, 1)

        _Intensity ("Emission Intensity", Range(0, 20)) = 5

        _FlowSpeed ("Flow Speed", Range(0, 10)) = 2
        _NoiseScale ("Noise Scale", Range(1, 30)) = 8
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.15

        _CorePower ("Core Concentration", Range(0.5, 10)) = 2

        _BaseFade ("Base Fade Start", Range(0, 0.5)) = 0.02
        _BaseFadeEnd ("Base Fade End", Range(0, 0.5)) = 0.12

        _TipFadeStart ("Tip Fade Start", Range(0, 1)) = 0.7
        _TipFadeEnd ("Tip Fade End", Range(0, 1)) = 0.98

        _EdgeFade ("Edge Fade", Range(0.01, 1)) = 0.3

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "UniversalForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float4 _CoreColor;
                float4 _OuterColor;

                float _Intensity;

                float _FlowSpeed;
                float _NoiseScale;
                float _NoiseStrength;

                float _CorePower;

                float _BaseFade;
                float _BaseFadeEnd;

                float _TipFadeStart;
                float _TipFadeEnd;

                float _EdgeFade;

                float _PulseSpeed;
                float _PulseAmount;

            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            // ---------------------------------
            // PROCEDURAL NOISE
            // ---------------------------------

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            // ---------------------------------
            // VERTEX SHADER
            // ---------------------------------

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;

                return output;
            }

            // ---------------------------------
            // FRAGMENT SHADER
            // ---------------------------------

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // Animated plasma noise

                float2 noiseUV = float2(
                    uv.x * _NoiseScale,
                    uv.y * _NoiseScale - time * _FlowSpeed
                );

                float noise = Noise(noiseUV);

                float distortion =
                    (noise - 0.5) * _NoiseStrength;

                float lengthUV = uv.y + distortion;

                // Fade at engine opening

                float baseMask = smoothstep(
                    _BaseFade,
                    _BaseFadeEnd,
                    lengthUV
                );

                // Fade at exhaust tip

                float tipMask = 1.0 - smoothstep(
                    _TipFadeStart,
                    _TipFadeEnd,
                    lengthUV
                );

                // Camera-facing edge fade

                float3 normalWS = normalize(input.normalWS);

                float3 viewDir = normalize(
                    GetWorldSpaceViewDir(input.positionWS)
                );

                float facing = saturate(
                    dot(normalWS, viewDir)
                );

                float edgeMask = smoothstep(
                    0.0,
                    _EdgeFade,
                    facing
                );

                // Bright inner plasma core

                float coreMask = pow(
                    facing,
                    _CorePower
                );

                // Blend plasma colours

                float3 plasmaColor = lerp(
                    _OuterColor.rgb,
                    _CoreColor.rgb,
                    coreMask
                );

                // Energy pulse

                float pulse = 1.0 +
                    sin(time * _PulseSpeed) * _PulseAmount;

                // Combine masks

                float alpha =
                    baseMask *
                    tipMask *
                    edgeMask;

                alpha *= saturate(
                    0.65 + noise * 0.7
                );

                // Final emission

                float emission = _Intensity * pulse;

                float3 finalColor =
                    plasmaColor * emission;

                return float4(
                    finalColor,
                    saturate(alpha)
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
