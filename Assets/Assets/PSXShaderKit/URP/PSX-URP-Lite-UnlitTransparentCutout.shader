Shader "PSX/URP/Lite Unlit Transparent Cutout"
{
    Properties
    {
        _Color("Color (RGBA)", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _ObjectDithering("Per-Object Dithering Enable", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        ZWrite On
        LOD 100

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex PSXVert
            #pragma fragment PSXFrag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #define PSX_CUTOUT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "PSX-URP-Lite-Common.hlsl"
            ENDHLSL
        }
    }
    Fallback "Hidden/UniversalRenderPipeline/FallbackError"
}
