Shader "Vefects/SH_Vefects_URP_Grid_01"
{
    Properties
    {
        _Masks("Masks", 2D) = "white" {}
        _Texture("Texture", 2D) = "white" {}
        _TileOverall("Tile Overall", Float) = 200
        _TileX("Tile X", Float) = 1
        _TileY("Tile Y", Float) = 1
        _TextureTileOverall("Texture Tile Overall", Float) = 1
        _Normal("Normal", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Float) = 1
        _RoughnessMin("Roughness Min", Float) = 0
        _RoughnessMax("Roughness Max", Float) = 1
        _Specular("Specular", Float) = 0.01
        _ColorOverall("Color Overall", Color) = (1, 1, 1, 1)
        _Color01("Color 01", Color) = (1, 1, 1, 1)
        _Color02("Color 02", Color) = (1, 1, 1, 1)
        _MasksTileOverall("Masks Tile Overall", Float) = 1
        _RandomTileColorsMax("Random Tile Colors Max", Float) = 1
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Masks_ST;
                float4 _Texture_ST;
                float4 _Normal_ST;
                float _TileOverall;
                float _TileX;
                float _TileY;
                float _TextureTileOverall;
                float _NormalIntensity;
                float _RoughnessMin;
                float _RoughnessMax;
                float _Specular;
                half4 _ColorOverall;
                half4 _Color01;
                half4 _Color02;
                float _MasksTileOverall;
                float _RandomTileColorsMax;
            CBUFFER_END

            TEXTURE2D(_Masks);
            SAMPLER(sampler_Masks);
            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);
            TEXTURE2D(_Normal);
            SAMPLER(sampler_Normal);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                // BIRP parity: tiled base UV, mask-driven two-tone tiles, normal-mapped spec floor.
                float2 tileUV = IN.uv * _TileOverall * float2(_TileX, _TileY);
                half4 tex = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, tileUV * _TextureTileOverall);
                half4 tileMasks = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, tileUV * _MasksTileOverall);
                float randT = lerp(0.0, _RandomTileColorsMax, saturate(tileMasks.b));
                half3 tileCol = lerp(_Color01.rgb, _Color02.rgb, saturate(tileMasks.r) * randT);
                half3 albedo = tex.r * tileCol * _ColorOverall.rgb;

                half3 nTex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, tileUV));
                // NOTE: tangent-space normal without full TBN (floor plane): perturb geometric normal.
                half3 N = normalize(IN.normalWS + (nTex.xxy - half3(0, 0, 1)) * _NormalIntensity);
                float smoothness = lerp(_RoughnessMin, _RoughnessMax, tex.g);

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float ndotl = saturate(dot(N, L));
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), lerp(8.0, 128.0, smoothness)) * _Specular * 8.0;
                half3 ambient = SampleSH(N) * 0.5 + 0.25;
                half3 col = albedo * (mainLight.color * ndotl + ambient) + mainLight.color * spec;
                return half4(col, 1.0);
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
