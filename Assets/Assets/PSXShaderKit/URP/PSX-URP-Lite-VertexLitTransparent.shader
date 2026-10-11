Shader "PSX/URP/Lite Vertex Lit Transparent"
{
    Properties
    {
        _Color("Color (RGBA)", Color) = (1, 1, 1, 1)
        _EmissionColor("Emission Color (RGBA)", Color) = (0,0,0,0)
        _CubemapColor("Cubemap Color (RGBA)", Color) = (0,0,0,0)
        _MainTex("Texture", 2D) = "white" {}
        _EmissiveTex("Emissive", 2D) = "black" {}
        _Cubemap("Cubemap", Cube) = "" {}
        _ReflectionMap("Reflection Map", 2D) = "white" {}
        _ObjectDithering("Per-Object Dithering Enable", Range(0,1)) = 1
        _FlatShading("Flat Shading", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
            #pragma multi_compile __ PSX_ENABLE_CUSTOM_VERTEX_LIGHTING
            #pragma multi_compile __ PSX_FLAT_SHADING_MODE_CENTER

            #define PSX_VERTEX_LIT
            #define PSX_CUBEMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "PSX-URP-Lite-Common.hlsl"
            ENDHLSL
        }
    }
    Fallback "Hidden/UniversalRenderPipeline/FallbackError"
}
