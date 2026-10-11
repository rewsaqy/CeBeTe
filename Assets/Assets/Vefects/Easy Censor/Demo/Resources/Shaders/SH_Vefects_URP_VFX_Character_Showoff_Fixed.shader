Shader "Vefects/SH_Vefects_URP_VFX_Character_Showoff_Fixed"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _EdgesColor("Edges Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity("Emission Intensity", Float) = 1
        _EdgesPower("Edges Power", Float) = 1
        _EdgesMultiply("Edges Multiply", Float) = 1
        _EdgesBias("Edges Bias", Float) = 0
        _BaseColorEdgesLerp("Base Color Edges Lerp", Float) = 0
        _WorldNoiseTiling("World Noise Tiling", Vector) = (1, 1, 0, 0)
        _WorldNoiseSpeed("World Noise Speed", Vector) = (0, 0, 0, 0)
        _NoiseEdgesTexture("Noise Edges Texture", 2D) = "white" {}
        _NoiseDepth("Noise Depth", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgesColor;
                float _EmissionIntensity;
                float _EdgesPower;
                float _EdgesMultiply;
                float _EdgesBias;
                float _BaseColorEdgesLerp;
                float4 _WorldNoiseTiling;
                float4 _WorldNoiseSpeed;
                float _NoiseDepth;
            CBUFFER_END

            TEXTURE2D(_NoiseEdgesTexture);
            SAMPLER(sampler_NoiseEdgesTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float fresnel = pow(1.0 - saturate(dot(N, V)), _EdgesPower);
                float edgeMask = saturate(saturate(fresnel * _EdgesMultiply) + _EdgesBias);

                float3 objOriginWS = unity_ObjectToWorld._m03_m13_m23;
                float3 rel = IN.positionWS - objOriginWS;
                float2 scrollUV = (rel.xy + edgeMask * _NoiseDepth) * _WorldNoiseTiling.xy
                    + _Time.y * _WorldNoiseSpeed.xy;
                half noise = SAMPLE_TEXTURE2D(_NoiseEdgesTexture, sampler_NoiseEdgesTexture, scrollUV).r;
                float edge = edgeMask * noise;

                half4 mid = lerp(_BaseColor, _EdgesColor, edge);
                half4 albedo = lerp(_BaseColor, mid, _BaseColorEdgesLerp);
                half3 emission = (_EdgesColor.rgb * edge) * _EmissionIntensity;
                return half4(albedo.rgb + emission, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
}
