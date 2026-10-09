// CLAUDE: the view through closed eyelids, for the intro cutscene.
// CLAUDE: Drawn on a full-screen UI RawImage inside the cutscene
// CLAUDE: prefab. Every value is driven from the Timeline, including
// CLAUDE: _T (time), so Speed x2 and scrubbing stay in sync.
// CLAUDE: Calm by design: slow pulse, low contrast, no flashes.
// CLAUDE: Look: compute "how much light passes the lids" (one number),
// CLAUDE: then colour it through a warm ramp, like light through skin.
Shader "Oyung/Cutscenes/ClosedEyes"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI)", 2D) = "white" {}
        _T ("Time (driven by Timeline)", Float) = 0
        _Brightness ("Brightness", Range(0, 1.5)) = 0.2
        _Warmth ("Warmth (cool dark -> skin red)", Range(0, 1)) = 0
        _PulseFreq ("Pulse per second (keep constant)", Range(0, 2)) = 1
        _PulseAmount ("Pulse amount", Range(0, 0.3)) = 0.08
        _Pressure ("Pressure (swell with pulse)", Range(0, 0.1)) = 0.03
        _Cloud ("Cloudy variation", Range(0, 1)) = 0.5
        _Vignette ("Vignette", Range(0, 1)) = 0.6
        _Grain ("Grain", Range(0, 0.1)) = 0.015
        // CLAUDE: light shapes: xy = screen pos (0..1), z = radius,
        // CLAUDE: w = strength; negative w = a dark silhouette.
        _Blob0 ("Light shape 0", Vector) = (0.3, 0.6, 0.25, 0)
        _Blob1 ("Light shape 1", Vector) = (0.7, 0.5, 0.2, 0)
        _Blob2 ("Light shape 2", Vector) = (0.5, 0.3, 0.3, 0)
        _Blob3 ("Light shape 3", Vector) = (0.2, 0.2, 0.15, 0)
        // CLAUDE: tree tops seen from below while being carried. Move
        // CLAUDE: them with _CanopyOffset in poses (not a speed).
        _Canopy ("Tree tops", Range(0, 1)) = 0
        _CanopyLevel ("Tree tops reach down", Range(0, 0.8)) = 0.3
        _CanopyOffset ("Tree tops position", Float) = 0
        _StepBob ("Step bob (tree tops only)", Range(0, 0.05)) = 0
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
            float _Pressure, _Cloud, _Vignette, _Grain;
            float _Canopy, _CanopyLevel, _CanopyOffset, _StepBob;
            float _Open, _LidSoftness;
            float4 _Blob0, _Blob1, _Blob2, _Blob3;

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

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x),
                            lerp(hash(i + float2(0, 1)), hash(i + 1), u.x),
                            u.y);
            }

            // CLAUDE: three octaves of slow "clouds": blood-warm light
            // CLAUDE: that is never perfectly even behind the lids.
            float clouds(float2 p)
            {
                return noise(p) * 0.55 + noise(p * 2.1 + 7.1) * 0.3
                     + noise(p * 4.3 + 3.7) * 0.15;
            }

            // CLAUDE: soft round light or shadow; sways a little. Move
            // CLAUDE: it with poses: a speed value can't crossfade (a
            // CLAUDE: speed times time jumps when the speed changes).
            float blob(float2 p, float4 b, float aspect, float seed)
            {
                float2 c = b.xy;
                c += 0.012 * float2(sin(_T * 0.7 + seed * 1.7),
                                    sin(_T * 0.5 + seed * 2.3));
                float2 d = p - c;
                d.x *= aspect;
                return b.w * exp(-dot(d, d) / max(b.z * b.z, 1e-4));
            }

            // CLAUDE: tree crowns seen from below while being carried:
            // CLAUDE: clumpy leaf masses with gaps of sky, denser toward
            // CLAUDE: the top and the left (looking up and sideways).
            // CLAUDE: Near + far layer (parallax). Both bob with the
            // CLAUDE: parent's steps at a fixed walking rhythm.
            float crowns(float2 p)
            {
                return noise(p) * 0.6 + noise(p * 2.3 + 5.2) * 0.3
                     + noise(p * 5.1 + 1.7) * 0.1;
            }

            float canopy(float2 uv, float aspect)
            {
                float step = sin(_T * 1.8 * 6.2831853);
                float2 q = float2(uv.x * aspect, uv.y);
                float edge = smoothstep(0.25, 1.0, uv.y) * 0.45
                           + smoothstep(0.6, 0.0, uv.x) * 0.3
                           + (_CanopyLevel - 0.3);

                float2 qn = q + float2(_CanopyOffset, _StepBob * step);
                float near = smoothstep(0.56, 0.78,
                                        crowns(qn * 2.6) + edge);
                float2 qf = q + float2(_CanopyOffset * 0.5,
                                       _StepBob * 0.5 * step);
                float far = smoothstep(0.6, 0.86,
                                       crowns(qf * 4.0 + 9.0) + edge * 0.8);
                return saturate(near + far * 0.5);
            }

            // CLAUDE: light through skin: black -> deep crimson ->
            // CLAUDE: red-orange -> pale peach where light is strongest.
            float3 skinRamp(float l)
            {
                float3 c0 = float3(0.015, 0.002, 0.005);
                float3 c1 = float3(0.30, 0.025, 0.035);
                float3 c2 = float3(0.72, 0.19, 0.08);
                float3 c3 = float3(1.0, 0.72, 0.52);
                float3 c = lerp(c0, c1, smoothstep(0.0, 0.35, l));
                c = lerp(c, c2, smoothstep(0.3, 0.75, l));
                return lerp(c, c3, smoothstep(0.75, 1.3, l));
            }

            // CLAUDE: before warmth: a muted, cool dark.
            float3 coolRamp(float l)
            {
                return float3(0.16, 0.12, 0.16) * smoothstep(0.0, 1.2, l);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 e = uv * 2.0 - 1.0;

                float beat = sin(_T * _PulseFreq * 6.2831853);
                float pulse = 1.0 + _PulseAmount * beat;

                // Pressure: the cloud field swells from the centre.
                float2 q = (uv - 0.5) * (1.0 - _Pressure * beat) + 0.5;
                float c = clouds(q * float2(aspect, 1) * 1.8 +
                                 _T * float2(0.025, 0.015));

                float light = _Brightness * pulse *
                              lerp(1.0, 0.55 + 0.9 * c, _Cloud);
                light += (blob(uv, _Blob0, aspect, 0)
                        + blob(uv, _Blob1, aspect, 1)
                        + blob(uv, _Blob2, aspect, 2)
                        + blob(uv, _Blob3, aspect, 3)) * pulse;

                // Tree tops block the light; gaps between them glow.
                light *= lerp(1.0, 0.12, canopy(uv, aspect) * _Canopy);

                // Vignette: the curve of the eyelid, darker at the rim.
                float v = smoothstep(1.35, 0.25, length(e * float2(0.85, 1.1)));
                light *= lerp(1.0, v, _Vignette);
                light = max(light, 0.0);

                float3 col = lerp(coolRamp(light), skinRamp(light), _Warmth);

                // CLAUDE: film grain, changes 12 times a second at very
                // CLAUDE: low strength (texture, not flicker).
                col += (hash(uv * _ScreenParams.xy + floor(_T * 12.0))
                        - 0.5) * _Grain;

                // CLAUDE: eyelids: an almond-shaped opening grows from
                // CLAUDE: the middle; inside it the game shows through.
                // CLAUDE: The lid rim is darker (lashes, shadow).
                float h = _Open * 2.0 * (1.0 - 0.5 * e.x * e.x)
                          - _LidSoftness;
                float y = abs(e.y);
                float lid = smoothstep(h, h + _LidSoftness, y);
                float rim = smoothstep(h, h + _LidSoftness * 3.0, y);
                col *= lerp(1.0, lerp(0.25, 1.0, rim), saturate(_Open * 4));

                return float4(col, lid * i.color.a);
            }
            ENDCG
        }
    }
}
