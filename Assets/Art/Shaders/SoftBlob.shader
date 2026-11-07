// CLAUDE: a soft round spot with no texture: droplets of a popped bubble
// CLAUDE: (colour from the particle), the shadow under a name letter, and
// CLAUDE: with a hole in it, the rings of an aiming mark.
Shader "Oyung/SoftBlob"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Softness ("Softness", Range(0.01, 1)) = 0.7
        _Hole ("Hole", Range(0, 0.95)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "SoftBlob"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Softness;
                half _Hole;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 colour : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 colour : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0;
                output.colour = input.colour;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half distance = length(input.uv);
                half spot = smoothstep(1.0, 1.0 - _Softness, distance)
                    * smoothstep(_Hole - 0.05, _Hole, distance);
                half4 colour = input.colour * _BaseColor;
                colour.a *= spot;
                return colour;
            }
            ENDHLSL
        }
    }
}
