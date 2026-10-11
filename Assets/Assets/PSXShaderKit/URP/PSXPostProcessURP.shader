Shader "Hidden/Yumiwa/PSXPostProcessURP"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "PSXPostUber"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            TEXTURE2D(_PreviousFrame);

            float _PixelationFactor;
            float3 _ColorResolution;
            float3 _DitherResolution;
            float _HighResDitherMatrix;
            float _DitheringScale;
            float _InterlacingSize;
            float _InterlacedFrameIndex;
            float _HistoryValid;
            float _InterlaceStatic;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            float DitherColorChannel(float color, float ditherOffset, float ditherStep)
            {
                float distance = fmod(color, ditherStep);
                float baseValue = floor(color / ditherStep) * ditherStep;
                float nudgedValue = (distance + ditherOffset * ditherStep) / ditherStep;
                return baseValue + ditherStep * floor(nudgedValue);
            }

            float DitherCol(float color, float ditherOffset, float ditherStep)
            {
                float distance = fmod(color, ditherStep);
                float baseValue = floor(color / ditherStep) * ditherStep;
                return (ditherOffset < distance / ditherStep - 0.001f) ? baseValue + ditherStep : baseValue;
            }

            float GetDitherThreshold(uint2 pixelPosition)
            {
                const int ditheringMatrix4x4[16] =
                {
                    0, 8, 2, 10,
                    12, 4, 14, 6,
                    3, 11, 1, 9,
                    15, 7, 13, 5
                };
                const int ditheringMatrix4x4PS1[16] =
                {
                    -4, +0, -3, +1,
                    +2, -2, +3, -1,
                    -3, +1, -4, +0,
                    +3, -1, +2, -2
                };
                const int ditheringMatrix2x2[4] =
                {
                    0, 3,
                    2, 1
                };
                uint ix4 = (pixelPosition.x % 4) + (pixelPosition.y % 4) * 4;
                uint ix2 = (pixelPosition.x % 2) + (pixelPosition.y % 2) * 2;
                return _HighResDitherMatrix > 0.25f ? _HighResDitherMatrix > 0.75f
                    ? ditheringMatrix4x4PS1[ix4] * 0.125f
                    : ditheringMatrix4x4[ix4] * 0.0625f
                    : ditheringMatrix2x2[ix2] * 0.25f;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 screenSize = _ScaledScreenParams.xy;
                float2 pixelPos = i.uv * screenSize;

                // 1. Pixelation (kit: grid = resolution * factor, point sampled).
                float2 grid = max(screenSize * _PixelationFactor, float2(1.0, 1.0));
                float2 puv = _PixelationFactor >= 1.0f ? i.uv : floor(i.uv * grid) / grid;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, puv);

                // 2. Color depth + ordered dither (kit Fullscreen_Customizable path).
                float3 colorStep = 1.0f / max(_ColorResolution, float3(1.0, 1.0, 1.0));
                float3 ditherStep = 1.0f / max(_DitherResolution, float3(1.0, 1.0, 1.0));
                uint2 ditherPx = (uint2)floor(pixelPos * _DitheringScale);
                float ditherThreshold = GetDitherThreshold(ditherPx);
                col.r = DitherCol(col.r, 0.5f, colorStep.r);
                col.g = DitherCol(col.g, 0.5f, colorStep.g);
                col.b = DitherCol(col.b, 0.5f, colorStep.b);
                col.r = DitherColorChannel(col.r, ditherThreshold, ditherStep.r);
                col.g = DitherColorChannel(col.g, ditherThreshold, ditherStep.g);
                col.b = DitherColorChannel(col.b, ditherThreshold, ditherStep.b);

                // 3. Interlacing: static scanlines, or temporal vs previous frame (kit mapping).
                if (_InterlacingSize > 0.5f)
                {
                    float row = floor(pixelPos.y / _InterlacingSize);
                    if (_InterlaceStatic > 0.5f)
                    {
                        col.rgb *= (fmod(row, 2.0f) < 1.0f) ? 1.0f : 0.3f;
                    }
                    else if (_HistoryValid > 0.5f)
                    {
                        half4 prev = SAMPLE_TEXTURE2D(_PreviousFrame, sampler_LinearClamp, i.uv);
                        float usePrev = fmod(row, 2.0f) == round(_InterlacedFrameIndex) ? 1.0f : 0.0f;
                        col = lerp(col, prev, usePrev);
                    }
                }
                return col;
            }
            ENDHLSL
        }
    }
}
