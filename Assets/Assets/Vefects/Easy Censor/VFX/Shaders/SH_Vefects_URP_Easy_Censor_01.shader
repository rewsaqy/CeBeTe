Shader "Vefects/SH_Vefects_URP_Easy_Censor_01"
{
    Properties
    {
        [Header(Pixelate)] _PixelateSize("Pixelate Size", Range(0, 1)) = 0.03
        [Header(Filters)] _HueShift("Hue Shift", Float) = 0
        _Saturation("Saturation", Float) = 1
        _LightnessValue("Lightness Value", Float) = 1
        [Header(Mask)] _Mask("Mask", 2D) = "white" {}
        _MaskErosion("Mask Erosion", Float) = 0
        _MaskErosionSmooth("Mask Erosion Smooth", Float) = 0.01
        [Header(Attributes)] _AlwaysOnTopDeactivated("Always On Top Deactivated", Float) = 0
        _Emission("Emission", Float) = 1
        _DepthFade("Depth Fade", Float) = 0
        [Header(Blend State)] _Cull("Cull", Float) = 2
        _Src("Src", Float) = 5
        _Dst("Dst", Float) = 10
        _ZWrite("ZWrite", Float) = 0
        _ZTest("ZTest", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_Src][_Dst]
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelateSize;
                float _HueShift;
                float _Saturation;
                float _LightnessValue;
                float4 _Mask_ST;
                float _MaskErosion;
                float _MaskErosionSmooth;
                float _AlwaysOnTopDeactivated;
                float _Emission;
                float _DepthFade;
            CBUFFER_END

            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float viewDepth : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.color = IN.color;
                OUT.uv = TRANSFORM_TEX(IN.uv, _Mask);
                OUT.screenPos = ComputeScreenPos(posInputs.positionCS);
                float3 viewPos = TransformWorldToView(posInputs.positionWS);
                OUT.viewDepth = max(-viewPos.z, 0.0001);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-4);

                // Pixelate the opaque frame (BIRP GrabPass equivalent).
                float2 pixelatedUV = screenUV;
                if (_PixelateSize > 0.0001)
                {
                    float2 grid = max(_PixelateSize * _ScaledScreenParams.xy, float2(1.0, 1.0));
                    pixelatedUV = floor(screenUV * grid) / grid;
                }

                half3 sceneColor = SampleSceneColor(pixelatedUV);
                sceneColor *= IN.color.rgb;

                float3 hsv = RGBToHSV(sceneColor);
                hsv.x = frac(hsv.x + _HueShift);
                hsv.y = saturate(hsv.y * _Saturation);
                hsv.z = hsv.z * _LightnessValue;
                half3 filtered = HSVToRGB(hsv);

                half maskSample = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, IN.uv).r;
                half eroded = smoothstep(_MaskErosion, _MaskErosion + max(_MaskErosionSmooth, 1e-4), maskSample);
                half alpha = saturate(eroded * IN.color.a);

                // Optional depth fade (BIRP DepthFade equivalent). 0 = disabled.
                float fade = 1.0;
                if (_DepthFade > 0.0001)
                {
                    float sceneRawDepth = SampleSceneDepth(screenUV);
                    float sceneEye = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                    fade = saturate((sceneEye - IN.viewDepth) / _DepthFade);
                }
                alpha = lerp(alpha, alpha * fade, saturate(_AlwaysOnTopDeactivated));

                half3 col = filtered * _Emission;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
