// CLAUDE: a letter like a child's painted wooden block: one soft colour
// CLAUDE: per letter (set per renderer), gently shaded from above, no scene
// CLAUDE: lights so it reads the same anywhere. _Highlight, when the gaze
// CLAUDE: is on it, brightens it and adds slow, soft sparkles.
Shader "Oyung/ToyLetter"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.9, 0.3, 0.3, 1)
        _Shading ("Soft shading", Range(0, 1)) = 0.35
        _Highlight ("Highlight (driven by code)", Range(0, 1)) = 0
        _HighlightGlow ("Highlight brightening", Range(0, 1)) = 0.25
        _SparkleColour ("Sparkle colour", Color) = (1, 0.97, 0.85, 1)
        _SparkleSize ("Sparkle size (higher is smaller)", Range(1, 40)) = 12
        _SparkleSpeed ("Sparkle speed", Range(0, 6)) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ToyLetter"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Shading;
                half _Highlight;
                half _HighlightGlow;
                half4 _SparkleColour;
                float _SparkleSize;
                float _SparkleSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalVS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalVS = TransformWorldToViewDir(
                    TransformObjectToWorldNormal(input.normalOS));
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            float Hash(float3 cell)
            {
                return frac(sin(dot(cell, float3(12.9898, 78.233, 37.719)))
                    * 43758.5453);
            }

            // CLAUDE: one possible sparkle per cell, each breathing in and
            // CLAUDE: out at its own moment, soft at the edges.
            float Sparkles(float3 position)
            {
                float3 p = position * _SparkleSize;
                float3 cell = floor(p);
                float seed = Hash(cell);
                // CLAUDE: off the cell's centre, or they'd form a grid.
                float3 scatter = float3(Hash(cell + 1.0), Hash(cell + 2.0),
                    Hash(cell + 3.0)) - 0.5;
                float3 local = frac(p) - 0.5 - scatter * 0.6;
                float breath = sin(_Time.y * _SparkleSpeed
                    + seed * TWO_PI) * 0.5 + 0.5;
                return smoothstep(0.5, 0.0, length(local))
                    * breath * breath * breath * step(0.55, seed);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalVS);
                half light = lerp(1.0, saturate(normal.y * 0.5 + 0.7),
                    _Shading);
                half3 colour = _BaseColor.rgb * light;
                colour = lerp(colour, 1.0, _Highlight * _HighlightGlow);
                colour += _SparkleColour.rgb * Sparkles(input.positionOS)
                    * _Highlight;
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}
