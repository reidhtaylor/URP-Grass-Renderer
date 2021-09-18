Shader "URP Grass/CrossGrassShader"
{
    Properties {
        _MainTex("Main Tex", 2D) = "white" {}
        _BladeBaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BladeTipColor("Tip Color", Color) = (1, 1, 1, 1)
        _TipStep("Tip Step", Range(0, 1)) = 0.35
        _AlphaClip("Alpha Clip", Range(0, 1)) = 0.35
        [Space]
        _AO("Ambient Occlusion", Range(0, 1)) = 0.4
        _AOStep("Ambient Occlusion Step", Range(0, 1)) = 0.25
        _AOColor("Ambient Occlusion Color", Color) = (0, 0, 0, 0)
        [Space]
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0, 1)) = 0.7
        _MinLightDarkness("Min Light Darkness", Range(0, 1)) = 0.1
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}
            Cull Off

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct DrawVertex {
                float3 positionWS;
                float2 uv;
            };
            struct DrawTriangle {
                float3 lightingNormalWS;
                DrawVertex vertices[3];
            };
            StructuredBuffer<DrawTriangle> _DrawTriangles;

            struct VertexOutput {
                float2 uv : TEXCOORD0;

                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                
                float4 positionCS : SV_POSITION;
            };

            float4 _BladeBaseColor;
            float4 _BladeTipColor;
            float _AlphaClip;

            float _AO;
            float _AOStep;
            float4 _AOColor;

            float _TipStep;

            float _ReceiveShadowStrength;
            float _MinLightDarkness;

            sampler2D _MainTex;

            VertexOutput Vertex(uint vertexID: SV_VertexID) {
                VertexOutput output = (VertexOutput)0;

                DrawTriangle tri = _DrawTriangles[vertexID / 3];
                DrawVertex input = tri.vertices[vertexID % 3];

                output.positionWS = input.positionWS;
                output.normalWS = tri.lightingNormalWS;
                output.uv = input.uv;
                output.positionCS = TransformWorldToHClip(input.positionWS);
                return output;
            }

            half4 Fragment (VertexOutput input) : SV_Target {
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = input.normalWS;
                lightingInput.viewDirectionWS = normalize(GetCameraPositionWS() - input.positionWS);
#if SHADOWS_SCREEN
                lightingInput.shadowCoord = ComputeScreenPos(input.positionCS);
#else
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
                
                float colorLerp = smoothstep((_TipStep * 2) - 1, 1, input.uv.y);
                float4 albedo = lerp(_BladeBaseColor.rgba, _BladeTipColor.rgba, colorLerp) * tex2D(_MainTex, input.uv);

                // Ambient Occlusion 
                albedo = lerp(albedo, _AOColor, smoothstep(_AOStep, 1, (1 - input.uv.y) * _AO * albedo.a * (1 - _AOColor)));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half shadow = mainLight.shadowAttenuation;
                shadow = max(shadow, 1 - _ReceiveShadowStrength);

                float lighting = (dot(input.normalWS, mainLight.direction) + 1) / 2;
                lighting = max(lighting, _MinLightDarkness);

                // float4 result = lighting;
                float4 result = albedo.rgba * lighting * shadow;
                // result.a = smoothstep(_AlphaClip, 1, albedo.a);
                result.a = 1;

                return result;
            }

            ENDHLSL
        }
    }
}
