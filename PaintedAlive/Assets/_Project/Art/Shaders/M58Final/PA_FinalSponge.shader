Shader "PaintedAlive/M58 Final/Tactile Sponge"
{
    Properties
    {
        _BaseColor("Dry Sponge", Color) = (0.72, 0.46, 0.12, 1)
        _AbsorbedColor("Absorbed Pigment", Color) = (0.34, 0.08, 0.12, 1)
        _Fill("Reservoir Fill", Range(0, 1)) = 0
        _Instability("Mixture Instability", Range(0, 1)) = 0
        _PoreScale("Pore Scale", Range(4, 80)) = 32
        _PoreDepth("Pore Depth", Range(0, 1)) = 0.55
        _DrySmoothness("Dry Smoothness", Range(0, 1)) = 0.16
        _WetSmoothness("Wet Smoothness", Range(0, 1)) = 0.68
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _AbsorbedColor;
                half _Fill;
                half _Instability;
                half _PoreScale;
                half _PoreDepth;
                half _DrySmoothness;
                half _WetSmoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float Pores(float2 uv)
            {
                float2 p = uv * _PoreScale;
                float2 cell = floor(p);
                float2 f = frac(p) - 0.5;
                float randomRadius = lerp(
                    0.12,
                    0.33,
                    Hash21(cell));
                float d = length(f +
                    (float2(Hash21(cell + 2.3), Hash21(cell + 7.1)) - 0.5) * 0.35);
                return 1.0 - smoothstep(
                    randomRadius,
                    randomRadius + 0.07,
                    d);
            }

            half3 PerturbNormal(
                half3 normalWS,
                float3 positionWS,
                float2 uv,
                float height)
            {
                float3 dpdx = ddx(positionWS);
                float3 dpdy = ddy(positionWS);
                float2 duvdx = ddx(uv);
                float2 duvdy = ddy(uv);
                float3 tangent = duvdy.y * dpdx - duvdx.y * dpdy;
                float3 bitangent = -duvdy.x * dpdx + duvdx.x * dpdy;
                float invLength = rsqrt(max(dot(tangent, tangent), dot(bitangent, bitangent)) + 1e-6);
                tangent *= invLength;
                bitangent *= invLength;
                return normalize(
                    normalWS -
                    tangent * ddx(height) * 3.0 -
                    bitangent * ddy(height) * 3.0);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float pores = Pores(input.uv);
                float stainNoise = Hash21(
                    floor(input.uv * float2(18.0, 23.0)));
                float instabilityMask =
                    saturate((stainNoise - 0.38) * 2.4) *
                    _Instability;
                half3 spongeColor = lerp(
                    _BaseColor.rgb,
                    _AbsorbedColor.rgb,
                    saturate(_Fill * (0.72 + stainNoise * 0.28)));
                spongeColor = lerp(
                    spongeColor,
                    spongeColor * half3(0.55, 0.72, 0.88),
                    instabilityMask * 0.35);
                spongeColor *= lerp(1.0, 0.44, pores * _PoreDepth);

                half3 normalWS = PerturbNormal(
                    normalize(input.normalWS),
                    input.positionWS,
                    input.uv,
                    -pores * _PoreDepth * 0.08);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = spongeColor;
                surface.metallic = 0;
                surface.specular = half3(0.04, 0.04, 0.04);
                surface.smoothness = saturate(
                    lerp(_DrySmoothness, _WetSmoothness, _Fill) -
                    pores * 0.12);
                surface.normalTS = half3(0, 0, 1);
                surface.emission = half3(0, 0, 0);
                surface.occlusion = lerp(1.0, 0.62, pores);
                surface.alpha = 1;
                surface.clearCoatMask = _Fill * 0.18;
                surface.clearCoatSmoothness = 0.86;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack Off
}
