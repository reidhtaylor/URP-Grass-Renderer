Shader "URP Grass/GrassShader"
{
    Properties {
        _BladeBaseColor("Base Color", Color) = (0, 0.5, 0, 1)
        _BladeTipColor("Tip Color", Color) = (0.54, 1, 0.54, 1)
        _TipStep("Tip Step", Range(0, 1)) = 0.35
        _AO("Ambient Occlusion", Range(0, 1)) = 0.4
        [Space]
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0, 1)) = 0.7
        _MinLightDarkness("Min Light Darkness", Range(0, 1)) = 0.1
        [Space]
        _HueNoise("Hue Noise", 2D) = "white" {}
        _HueScale("Hue Scale", Float) = 0.02
        _HueColor("Hue Color", Color) = (0.55, 0.415, 0, 1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

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
            #include "GrassBladesGraphicsHelp.hlsl"

            struct DrawVertex {
                float3 positionWS;
                float height;
            };
            struct DrawTriangle {
                float3 lightingNormalWS;
                DrawVertex vertices[3];
            };
            StructuredBuffer<DrawTriangle> _DrawTriangles;

            struct VertexOutput {
                float uv : TEXCOORD0;

                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                
                float4 positionCS : SV_POSITION;
            };

            float4 _BladeBaseColor;
            float4 _BladeTipColor;
            float _AO;

            float _TipStep;

            float _ReceiveShadowStrength;
            float _MinLightDarkness;

            sampler2D _HueNoise;
            float _HueScale;
            float4 _HueColor;

            VertexOutput Vertex(uint vertexID: SV_VertexID) {
                VertexOutput output = (VertexOutput)0;

                DrawTriangle tri = _DrawTriangles[vertexID / 3];
                DrawVertex input = tri.vertices[vertexID % 3];

                output.positionWS = input.positionWS;
                output.normalWS = tri.lightingNormalWS;
                output.uv = input.height;
                output.positionCS = TransformWorldToHClip(input.positionWS);
                return output;
            }

            half4 Fragment (VertexOutput input) : SV_Target {
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = input.normalWS;
                lightingInput.viewDirectionWS = GetViewDirectionFromPosition(input.positionWS);
                lightingInput.shadowCoord = CalculateShadowCoord(input.positionWS, input.positionCS);
                
                float colorLerp = smoothstep((_TipStep * 2) - 1, 1, input.uv);
                float4 albedo = lerp(_BladeBaseColor.rgba, _BladeTipColor.rgba, colorLerp);
                // Hue
                albedo += (1 - tex2D(_HueNoise, input.positionWS.xz * _HueScale)) * _HueColor;

                // Ambient Occlusion 
                albedo = lerp(albedo, float4(0.1, 0.1, 0.1, 1), (1 - input.uv) * _AO);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half shadow = mainLight.shadowAttenuation;
                shadow = max(shadow, 1 - _ReceiveShadowStrength);

                float lighting = (dot(input.normalWS, mainLight.direction) + 1) / 2;
                lighting = max(lighting, _MinLightDarkness);

                // float4 result = lighting;
                float4 result = albedo.rgba * lighting * shadow;
                result.a = 1;

                return result;
            }

            ENDHLSL
        }
    }
}
