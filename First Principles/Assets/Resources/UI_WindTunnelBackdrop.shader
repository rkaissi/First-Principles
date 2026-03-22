// Procedural wind-tunnel mood (aerospace stages): left→right freestream,
// smoke/schlieren streaks, faint test-section grid, subtle symmetric airfoil silhouette.
Shader "UI/WindTunnelBackdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Dummy", 2D) = "white" {}
        _Color ("Tint & alpha", Color) = (0.9, 0.96, 1, 0.54)
        _FlowSpeed ("Flow speed", Float) = 0.55
        _StreakScale ("Streak scale", Float) = 9
        _Aspect ("UV width / height", Float) = 1.7
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

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FlowSpeed;
            float _StreakScale;
            float _Aspect;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.texcoord;
                return o;
            }

            float hash21(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453123);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float s = 0.0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    s += a * noise2(p);
                    p *= 2.02;
                    a *= 0.5;
                }
                return s;
            }

            // Thin symmetric airfoil thickness (teaching shape): y_t(x) ~ semi-ellipse chord.
            float airfoilThickness(float xNorm)
            {
                xNorm = abs(xNorm);
                if (xNorm >= 1.0)
                    return 0.0;
                return 0.18 * sqrt(1.0 - xNorm * xNorm);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y * _FlowSpeed;
                float asp = max(_Aspect, 0.25);

                float2 p = (uv - 0.5) * 2.0;
                p.x *= asp;

                // Freestream along +x (wind enters from left of screen).
                float2 flow = float2(uv.x * asp * _StreakScale * 0.35 + t * 0.9, uv.y * _StreakScale * 0.55);
                float sm = fbm(flow);
                float sm2 = fbm(flow * 1.6 + float2(3.1, -t * 0.4));

                // Streaks: elongated along x.
                float streak = pow(sm * sm2, 1.4);
                streak *= smoothstep(0.15, 0.55, streak);

                // Wake wobble behind nominal chord (x in p space ~ -0.2 to 0.5).
                float chordX = p.x / 0.62;
                float yt = airfoilThickness(chordX);
                float distWing = abs(p.y) - yt;
                float wingMask = 1.0 - smoothstep(-0.02, 0.06, distWing);
                float wake = smoothstep(0.05, 0.45, p.x) * exp(-abs(p.y) * 2.2) * (1.0 - smoothstep(-0.35, 0.55, p.x));

                float turbWake = fbm(float2(p.x * 4.0 - t * 1.2, p.y * 8.0 + sin(p.x * 6.0 + t))) * wake * 0.55;

                // Test-section vertical rails (window frames).
                float rail = smoothstep(0.02, 0.0, abs(uv.x - 0.08)) + smoothstep(0.02, 0.0, abs(uv.x - 0.92));
                rail *= 0.15;

                // Cool tunnel base gradient (floor darker) — slightly lifted for readability.
                float3 baseCol = lerp(float3(0.06, 0.1, 0.15), float3(0.13, 0.18, 0.26), uv.y);
                baseCol += float3(0.08, 0.12, 0.17) * (1.0 - uv.y) * rail;

                float3 mist = float3(0.58, 0.76, 0.98) * streak * 0.72;
                mist += float3(0.5, 0.66, 0.92) * turbWake * 1.12;
                mist += float3(0.32, 0.42, 0.58) * wingMask * 0.16;

                float3 col = baseCol + mist;
                col = saturate(col);

                float a = _Color.a * i.color.a;
                return fixed4(col * _Color.rgb * i.color.rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
