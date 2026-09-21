Shader "PaintedAlive/M58 Final/Impasto"
{
    Properties
    {
        _BaseColor("Pigment", Color) = (0.56, 0.11, 0.13, 1)
        _EdgeColor("Pigment Edge", Color) = (0.24, 0.025, 0.04, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.86
        _Metallic("Metallic", Range(0, 1)) = 0.02
        _Dryness("Dryness", Range(0, 1)) = 0
        _GrooveScale("Bristle Groove Scale", Range(4, 64)) = 28
        _GrooveStrength("Bristle Relief", Range(0, 1)) = 0.42
        _EdgePigment("Edge Pigment", Range(0, 1)) = 0.55
        _CrackStrength("Dry Crack Strength", Range(0, 1)) = 0.72
        _CreationPulse("Creation Pulse", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 300

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
                half4 _EdgeColor;
                half _Smoothness;
                half _Metallic;
                half _Dryness;
                half _GrooveScale;
                half _GrooveStrength;
                half _EdgePigment;
                half _CrackStrength;
                half _CreationPulse;
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float GrooveHeight(float2 uv)
            {
                float bend = ValueNoise(float2(uv.y * 8.0, 0.31)) * 1.8;
                float broad = sin((uv.x * _GrooveScale + bend) * 6.2831853);
                float fine = sin((uv.x * _GrooveScale * 2.37 + uv.y * 3.1) * 6.2831853);
                return broad * 0.72 + fine * 0.28;
            }

            float DryCracks(float2 uv)
            {
                float2 p = uv * float2(11.0, 31.0);
                float row = floor(p.y);
                float drift = Hash21(float2(row, 4.7)) * 0.8;
                float vertical = abs(frac(p.x + drift + sin(p.y * 0.31) * 0.2) - 0.5);
                float horizontal = abs(frac(p.y + Hash21(float2(floor(p.x), 7.1))) - 0.5);
                float veins = min(vertical, horizontal * 1.45);
                return 1.0 - smoothstep(0.025, 0.085, veins);
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
                float slopeX = ddx(height) * 4.0;
                float slopeY = ddy(height) * 4.0;
                return normalize(normalWS - tangent * slopeX - bitangent * slopeY);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.shadowCoord =
                    GetShadowCoord(positionInputs);
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                float groove = GrooveHeight(uv);
                float micro = ValueNoise(uv * float2(37.0, 113.0));
                float edgeDistance = min(uv.x, 1.0 - uv.x);
                float edge = 1.0 - smoothstep(0.015, 0.23, edgeDistance);
                float cracks = DryCracks(uv) * _Dryness * _CrackStrength;

                half3 pigment = lerp(
                    _BaseColor.rgb,
                    _EdgeColor.rgb,
                    saturate(edge * _EdgePigment));
                pigment *= lerp(0.91, 1.08, groove * 0.5 + 0.5);
                pigment *= lerp(0.97, 1.03, micro);
                pigment = lerp(pigment, pigment * 0.28, cracks);

                float relief =
                    groove * _GrooveStrength * 0.08 +
                    micro * 0.015 -
                    cracks * 0.04;
                half3 normalWS = PerturbNormal(
                    normalize(input.normalWS),
                    input.positionWS,
                    uv,
                    relief);

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
                surface.albedo = pigment;
                surface.metallic = _Metallic;
                surface.specular = half3(0.04, 0.04, 0.04);
                surface.smoothness = saturate(
                    lerp(_Smoothness, min(_Smoothness, 0.24), _Dryness) -
                    cracks * 0.18 +
                    edge * (1.0 - _Dryness) * 0.08 +
                    _CreationPulse * 0.14);
                surface.normalTS = half3(0, 0, 1);
                surface.emission =
                    pigment * (_CreationPulse * 0.055);
                surface.occlusion = lerp(1.0, 0.72, cracks);
                surface.alpha = 1.0;
                surface.clearCoatMask =
                    saturate((1.0 - _Dryness) * 0.42);
                surface.clearCoatSmoothness = 0.92;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
