// CLAUDE: the view through closed eyelids, for the intro cutscene.
// CLAUDE: Drawn on a full-screen UI RawImage inside the cutscene
// CLAUDE: prefab. Every value is driven from the Timeline, including
// CLAUDE: _T (time), so Speed x2 and scrubbing stay in sync.
// CLAUDE: Calm by design: slow pulse, low contrast, no flashes.
Shader "Oyung/Cutscenes/ClosedEyes"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI)", 2D) = "white" {}
        _T ("Time (driven by Timeline)", Float) = 0
        _Brightness ("Brightness", Range(0, 1)) = 0.2
        _Warmth ("Warmth (dark -> red)", Range(0, 1)) = 0
        _PulseFreq ("Pulse per second", Range(0, 2)) = 1
        _PulseAmount ("Pulse amount", Range(0, 0.3)) = 0.08
        _Cloud ("Cloudy variation", Range(0, 1)) = 0.4
        _Grain ("Grain", Range(0, 0.1)) = 0.02
        _BlobColor ("Light shapes colour", Color) = (1, 0.85, 0.7, 1)
        // CLAUDE: light shapes: xy = screen pos (0..1), z = radius,
        // CLAUDE: w = strength. Timeline moves them.
        _Blob0 ("Light shape 0", Vector) = (0.3, 0.6, 0.25, 0)
        _Blob1 ("Light shape 1", Vector) = (0.7, 0.5, 0.2, 0)
        _Blob2 ("Light shape 2", Vector) = (0.5, 0.3, 0.3, 0)
        _Blob3 ("Light shape 3", Vector) = (0.2, 0.2, 0.15, 0)
        _Open ("Eyelids open", Range(0, 1)) = 0
        _LidSoftness ("Eyelid edge softness", Range(0.01, 0.5)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float _T, _Brightness, _Warmth, _PulseFreq, _PulseAmount;
            float _Cloud, _Grain, _Open, _LidSoftness;
            float4 _BlobColor, _Blob0, _Blob1, _Blob2, _Blob3;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // CLAUDE: smooth value noise, two octaves: the slow "clouds"
            // CLAUDE: of blood-warm light behind the lids.
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x),
                            lerp(hash(i + float2(0, 1)), hash(i + 1), u.x),
                            u.y);
            }

            float clouds(float2 p)
            {
                return noise(p) * 0.65 + noise(p * 2.3 + 7.1) * 0.35;
            }

            // CLAUDE: a soft round light, gaussian falloff, no hard edge.
            float blob(float2 p, float4 b, float aspect)
            {
                float2 d = p - b.xy;
                d.x *= aspect;
                return b.w * exp(-dot(d, d) / max(b.z * b.z, 1e-4));
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;

                // Warm haze: near-black to blood red, slow pulse.
                float3 dark = float3(0.03, 0.005, 0.01);
                float3 red = float3(0.6, 0.09, 0.05);
                float3 haze = lerp(dark, red, _Warmth);
                float pulse = 1.0 +
                    _PulseAmount * sin(_T * _PulseFreq * 6.2831853);
                float c = clouds(uv * float2(aspect, 1) * 2.0 +
                                 _T * float2(0.03, 0.02));
                float3 col = haze * _Brightness * pulse *
                             lerp(1.0, 0.6 + 0.8 * c, _Cloud);

                // Light shapes, seen through red skin: tinted warm.
                float l = blob(uv, _Blob0, aspect) + blob(uv, _Blob1, aspect)
                        + blob(uv, _Blob2, aspect) + blob(uv, _Blob3, aspect);
                float3 lidTint = lerp(float3(1, 1, 1), float3(1, 0.35, 0.2),
                                      0.4 + 0.4 * _Warmth);
                col += _BlobColor.rgb * lidTint * l * pulse;

                // CLAUDE: film grain, changes 12 times a second at very
                // CLAUDE: low strength (texture, not flicker).
                col += (hash(uv * _ScreenParams.xy + floor(_T * 12.0))
                        - 0.5) * _Grain;

                // CLAUDE: eyelids: an almond-shaped opening grows from
                // CLAUDE: the middle; inside it the game shows through.
                float2 e = uv * 2.0 - 1.0;
                float h = _Open * 2.0 * (1.0 - 0.5 * e.x * e.x)
                          - _LidSoftness;
                float lid = smoothstep(h, h + _LidSoftness, abs(e.y));

                return float4(col, lid * i.color.a);
            }
            ENDCG
        }
    }
}
