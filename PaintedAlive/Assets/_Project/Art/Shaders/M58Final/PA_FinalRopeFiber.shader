Shader "PaintedAlive/M58 Final/Rope Fiber"
{
    Properties
    {
        _BaseColor("Fiber Tint", Color) = (0.48, 0.19, 0.055, 1)
        _FiberColor("Raised Fiber", Color) = (0.68, 0.42, 0.17, 1)
        _FiberScale("Fiber Scale", Range(2, 80)) = 34
        _FiberStrength("Fiber Contrast", Range(0, 1)) = 0.38
        _Smoothness("Smoothness", Range(0, 1)) = 0.18
        _RimStrength("Rim Strength", Range(0, 1)) = 0.18
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
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
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
                half4 _FiberColor;
                half _FiberScale;
                half _FiberStrength;
                half _Smoothness;
                half _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
                half fogFactor : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

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
                output.color = input.color;
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float twist = sin(
                    (input.uv.x * _FiberScale +
                     input.uv.y * _FiberScale * 0.31) *
                    6.2831853);
                float fine = sin(
                    (input.uv.x * _FiberScale * 3.1 -
                     input.uv.y * _FiberScale * 0.77) *
                    6.2831853);
                float fibers =
                    twist * 0.72 + fine * 0.28;
                half3 viewDirection =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim = pow(
                    1.0 - saturate(dot(
                        normalize(input.normalWS),
                        viewDirection)),
                    2.2);

                half fiberMask = saturate(fibers * 0.5 + 0.5);
                half3 albedo = lerp(
                    _BaseColor.rgb,
                    _FiberColor.rgb,
                    fiberMask * _FiberStrength) * input.color.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = viewDirection;
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.metallic = 0;
                surface.specular = half3(0.04, 0.04, 0.04);
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.emission = rim * _RimStrength * albedo;
                surface.occlusion = lerp(0.74, 1.0, fiberMask);
                surface.alpha = _BaseColor.a * input.color.a;
                surface.clearCoatMask = 0;
                surface.clearCoatSmoothness = 0;

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
