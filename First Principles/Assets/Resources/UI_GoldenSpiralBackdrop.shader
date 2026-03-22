// Procedural logarithmic spiral mood (golden ratio boss): φ-growth arms in polar view,
// warm gold on deep field — matches r ∝ φ^{kθ} teaching language on the stage.
Shader "UI/GoldenSpiralBackdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Dummy", 2D) = "white" {}
        _Color ("Tint & alpha", Color) = (1, 0.85, 0.45, 0.48)
        _Aspect ("UV width / height", Float) = 1.7
        _TimeLive ("Time", Float) = 0
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
            float _Aspect;
            float _TimeLive;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float asp = max(_Aspect, 0.25);
                float2 p = (uv - 0.5) * 2.0;
                p.x *= asp;

                float r = length(p);
                float ang = atan2(p.y, p.x);

                // Golden ratio φ; log-spiral with one arm per 2π matches r ∝ φ^{θ/(2π)}.
                float phi = 1.618033988749;
                float twoPi = 6.28318530718;
                float b = log(phi) / twoPi;

                float t = _TimeLive * 0.22;

                float phase1 = log(r + 0.07) - b * ang;
                float spiral1 = abs(frac(phase1 * 2.15 + t * 0.045) - 0.5);

                float phase2 = log(r + 0.09) - b * (ang + 2.399963); // ~ golden angle
                float spiral2 = abs(frac(phase2 * 2.15 - t * 0.032) - 0.5);

                float phase3 = log(r + 0.05) - b * (-ang + 0.73);
                float spiral3 = abs(frac(phase3 * 1.85 + t * 0.028) - 0.5);

                float arms =
                    smoothstep(0.13, 0.0, spiral1) +
                    smoothstep(0.13, 0.0, spiral2) * 0.72 +
                    smoothstep(0.11, 0.0, spiral3) * 0.45;

                float radPhase = log(r + 0.04) / log(phi) * 2.8 + t * 0.06;
                float rings = smoothstep(0.09, 0.0, abs(frac(radPhase) - 0.5)) * 0.35;

                float vign = smoothstep(1.55, 0.28, r);
                float core = smoothstep(0.02, 0.14, r);

                float3 bgDeep = float3(0.05, 0.04, 0.1);
                float3 bgWarm = float3(0.12, 0.07, 0.04);
                float3 baseCol = lerp(bgDeep, bgWarm, saturate(r * 0.35 + uv.y * 0.25));

                float3 goldHi = float3(1.0, 0.9, 0.42);
                float3 goldMid = float3(0.92, 0.62, 0.22);
                float3 glow = lerp(goldMid, goldHi, arms * 0.65 + rings * 0.4);

                float intensity = (arms * 0.62 + rings * 0.28) * vign * (0.35 + 0.65 * core);
                float3 col = baseCol + glow * intensity;

                col = saturate(col);
                float a = _Color.a * i.color.a;
                return fixed4(col * _Color.rgb * i.color.rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
