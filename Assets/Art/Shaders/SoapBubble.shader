// CLAUDE: a soap bubble, rebuilt from the AE Tuts Shader Graph tutorial
// CLAUDE: "Bubbles in Unity using Shader Graph". Same recipe:
// CLAUDE: Alpha = min(reflection brightness, fresnel) + a floor, clamped,
// CLAUDE: so dark parts of the reflection are more see-through.
// CLAUDE: Colour = reflection blended with a gradient read by fresnel;
// CLAUDE: triplanar noise is subtracted from fresnel first, which makes
// CLAUDE: the coloured splashes. Noise drift and the Look group are ours.
// CLAUDE: The tutorial's gradient colours aren't in its text: ours are
// CLAUDE: a first guess, tune them in the material.
Shader "Oyung/SoapBubble"
{
    Properties
    {
        [Header(Look)]
        _Tint ("Tint colour", Color) = (0.25, 0.5, 1, 1)
        _TintAmount ("Tint amount (more blue)", Range(0, 1)) = 0
        _HueShift ("Hue shift", Range(-0.5, 0.5)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Shade ("Shade (darker)", Range(0, 1)) = 0
        _Translucency ("Translucency", Range(0, 1)) = 0

        [Header(Gradient from centre to rim)]
        _Colour0 ("Centre", Color) = (0.30, 0.25, 0.85, 1)
        _Colour1 ("Inner", Color) = (0.15, 0.80, 0.70, 1)
        _Colour2 ("Outer", Color) = (0.95, 0.30, 0.65, 1)
        _Colour3 ("Rim", Color) = (1.00, 0.85, 0.35, 1)
        _GradientBlend ("Gradient over reflection", Range(0, 1)) = 0.75

        [Header(Fresnel and alpha)]
        _FresnelPower ("Fresnel power", Range(0.1, 8)) = 2
        _AlphaFloor ("Alpha floor", Range(0, 1)) = 0.08

        [Header(Reflection)]
        _ReflectionStrength ("Reflection strength", Range(0, 4)) = 1

        [Header(Splashes)]
        _NoiseScale ("Size (higher is smaller)", Range(0.1, 30)) = 5
        _NoiseSubtract ("Amount", Range(0, 1)) = 0.3
        _NoiseSpeed ("Drift speed", Range(0, 2)) = 0.1
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
            Name "SoapBubble"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _TintAmount;
                half _HueShift;
                half _Saturation;
                half _Shade;
                half _Translucency;
                half4 _Colour0;
                half4 _Colour1;
                half4 _Colour2;
                half4 _Colour3;
                half _GradientBlend;
                half _FresnelPower;
                half _AlphaFloor;
                half _ReflectionStrength;
                float _NoiseScale;
                half _NoiseSubtract;
                float _NoiseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }

            // CLAUDE: the same gradient noise as Shader Graph's node.
            float2 NoiseDirection(float2 p)
            {
                p = fmod(p, 289.0);
                float x = fmod((34.0 * p.x + 1.0) * p.x, 289.0) + p.y;
                x = fmod((34.0 * x + 1.0) * x, 289.0);
                x = frac(x / 41.0) * 2.0 - 1.0;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float GradientNoise(float2 uv)
            {
                float2 ip = floor(uv);
                float2 fp = frac(uv);
                float d00 = dot(NoiseDirection(ip), fp);
                float d01 = dot(NoiseDirection(ip + float2(0, 1)),
                    fp - float2(0, 1));
                float d10 = dot(NoiseDirection(ip + float2(1, 0)),
                    fp - float2(1, 0));
                float d11 = dot(NoiseDirection(ip + float2(1, 1)),
                    fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6.0 - 15.0) + 10.0);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x)
                    + 0.5;
            }

            // CLAUDE: three noises by object position, blended by normal:
            // CLAUDE: no seams on a sphere, and it turns with the object.
            float TriplanarNoise(float3 position, float3 normal)
            {
                float3 weights = abs(normal);
                weights /= max(weights.x + weights.y + weights.z, 0.0001);
                float3 p = position * _NoiseScale + _Time.y * _NoiseSpeed;
                return GradientNoise(p.yz) * weights.x
                    + GradientNoise(p.xz) * weights.y
                    + GradientNoise(p.xy) * weights.z;
            }

            half3 SampleGradient(half t)
            {
                t = saturate(t) * 3.0;
                if (t < 1.0) return lerp(_Colour0.rgb, _Colour1.rgb, t);
                if (t < 2.0) return lerp(_Colour1.rgb, _Colour2.rgb, t - 1.0);
                return lerp(_Colour2.rgb, _Colour3.rgb, t - 2.0);
            }

            half3 ApplyHueShift(half3 colour, half shift)
            {
                // CLAUDE: Rodrigues' rotation around the grey axis.
                const half3 axis = half3(0.57735, 0.57735, 0.57735);
                float angle = shift * TWO_PI;
                return colour * cos(angle)
                    + cross(axis, colour) * sin(angle)
                    + axis * dot(axis, colour) * (1.0 - cos(angle));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normal = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

                // CLAUDE: world view and normal: the reflection stays put
                // CLAUDE: while the bubble turns.
                half3 reflection = GlossyEnvironmentReflection(
                    reflect(-view, normal), input.positionWS, 0, 1, screenUV)
                    * _ReflectionStrength;

                half fresnel = pow(1.0 - saturate(dot(normal, view)),
                    _FresnelPower);

                // CLAUDE: alpha: dark reflection and centre see-through.
                half alpha = saturate(min(length(reflection), fresnel)
                    + _AlphaFloor) * (1.0 - _Translucency);

                // CLAUDE: colour: noise pushes the gradient lookup around.
                half noise = saturate(
                    TriplanarNoise(input.positionOS, input.normalOS));
                half lookup = fresnel - noise * _NoiseSubtract;
                half3 colour = lerp(reflection, SampleGradient(lookup),
                    _GradientBlend);

                // CLAUDE: the Look group, in the order an artist expects.
                colour = ApplyHueShift(colour, _HueShift);
                half grey = dot(colour, half3(0.2126, 0.7152, 0.0722));
                colour = max(lerp(grey.xxx, colour, _Saturation), 0);
                colour = lerp(colour, colour * _Tint.rgb * 2.0, _TintAmount);
                colour *= 1.0 - _Shade;

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
