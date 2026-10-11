Shader "Vefects/SH_Vefects_URP_Extra_Billboard_01"
{
    Properties
    {
        _Texture("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _EmissionBoost("Emission Boost", Float) = 1
        [Enum(Off, 0, Front, 1, Back, 2)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull]
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                half4 _Color;
                float _EmissionBoost;
            CBUFFER_END

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = TRANSFORM_TEX(IN.uv, _Texture);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, IN.uv);
                half3 col = tex.rgb * _Color.rgb * IN.color.rgb * _EmissionBoost;
                half alpha = tex.a * _Color.a * IN.color.a;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
