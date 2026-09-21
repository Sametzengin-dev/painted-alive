Shader "PaintedAlive/M58 Final/Stroke Telegraph"
{
    Properties
    {
        _BaseColor("Telegraph Color", Color) = (0.95, 0.18, 0.16, 0.72)
        _EdgeColor("Bristle Edge", Color) = (1, 0.52, 0.24, 0.9)
        _FlowSpeed("Flow Speed", Range(0, 6)) = 2.1
        _DashScale("Dash Scale", Range(1, 32)) = 10
        _DashFill("Dash Fill", Range(0.1, 0.9)) = 0.65
        _EdgeSoftness("Edge Softness", Range(0.01, 0.48)) = 0.18
        _PulseStrength("Pulse Strength", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half _FlowSpeed;
                half _DashScale;
                half _DashFill;
                half _EdgeSoftness;
                half _PulseStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float edgeDistance = abs(input.uv.y - 0.5) * 2.0;
                float edgeFade = 1.0 - smoothstep(
                    1.0 - _EdgeSoftness,
                    1.0,
                    edgeDistance);
                float flow = input.uv.x * _DashScale -
                    _Time.y * _FlowSpeed;
                float dash = smoothstep(
                    1.0 - _DashFill - 0.12,
                    1.0 - _DashFill + 0.12,
                    sin(flow * 6.2831853) * 0.5 + 0.5);
                float pulse = 1.0 +
                    sin(_Time.y * 4.2) * _PulseStrength;

                half bristleEdge = saturate(edgeDistance * edgeDistance);
                half4 color = lerp(
                    _BaseColor,
                    _EdgeColor,
                    bristleEdge * 0.62) * input.color;
                color.rgb *= lerp(0.82, 1.18, dash) * pulse;
                color.a *= edgeFade * lerp(0.58, 1.0, dash);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
