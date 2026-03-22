// GPU backdrop for the gravity-well boss: Schwarzschild equatorial null geodesics.
// Math: photon Binet equation d²u/dφ² + u = 3 M u² with u = 1/r (G = c = 1).
// First integral (conserved b = L/E): (du/dφ)² = (1−2Mu)²/b² − u² + 2M u³.
// Ray is stepped in φ; emission samples a thin equatorial accretion disk (multiple passes = lensed images).
// Not full Kerr (Gargantua is rotating); Schwarzschild is the documented GR core here.
Shader "UI/BlackHoleGargantuaBackdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Dummy", 2D) = "white" {}
        _Color ("Multiply", Color) = (1, 1, 1, 0.78)
        _Mass ("M (geometric units, Rs=2M)", Float) = 1
        _CamDist ("Observer Schwarzschild r", Float) = 16
        _BMin ("Impact param b min", Float) = 3.3
        _BMax ("Impact param b max", Float) = 14
        _DiskIn ("Disk inner r / M", Float) = 3.35
        _DiskOut ("Disk outer r / M", Float) = 9
        _DiskSpin ("Disk phase speed", Float) = 0.35
        _DiskBright ("Disk emissivity", Float) = 2.4
        _PhotonRing ("Photon ring boost", Float) = 1.15
        _Dphi ("φ step (radians)", Float) = 0.045
        _Steps ("Integration steps", Int) = 88
        _Aspect ("UV width / height", Float) = 1.7
        _TimeLive ("Time (set from C#)", Float) = 0
        _StarField ("Star field strength", Float) = 0.085
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Mass;
            float _CamDist;
            float _BMin;
            float _BMax;
            float _DiskIn;
            float _DiskOut;
            float _DiskSpin;
            float _DiskBright;
            float _PhotonRing;
            float _Dphi;
            int _Steps;
            float _Aspect;
            float _TimeLive;
            float _StarField;

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

            // Cheap hash for star speckle (not physical, background only).
            float n2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            void rk4Step(inout float u, inout float v, inout float phi, float M, float h)
            {
                float h2 = h * 0.5;
                float k1u = v;
                float k1v = 3.0 * M * u * u - u;
                float u2 = u + h2 * k1u;
                float v2 = v + h2 * k1v;
                float k2u = v2;
                float k2v = 3.0 * M * u2 * u2 - u2;
                u2 = u + h2 * k2u;
                v2 = v + h2 * k2v;
                float k3u = v2;
                float k3v = 3.0 * M * u2 * u2 - u2;
                u2 = u + h * k3u;
                v2 = v + h * k3v;
                float k4u = v2;
                float k4v = 3.0 * M * u2 * u2 - u2;
                u += h * (k1u + 2.0 * k2u + 2.0 * k3u + k4u) / 6.0;
                v += h * (k1v + 2.0 * k2v + 2.0 * k3v + k4v) / 6.0;
                phi += h;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float M = max(_Mass, 0.05);
                float Rs = 2.0 * M;
                float rObs = max(_CamDist, Rs * 1.5);
                float u = 1.0 / rObs;
                float b = lerp(_BMin, _BMax, uv.x);
                b = max(b, Rs * 1.01);
                float fMu = 1.0 - 2.0 * M * u;
                if (fMu <= 0.001)
                    return fixed4(0, 0, 0, _Color.a * i.color.a);

                float disc = (1.0 / (b * b)) * fMu * fMu - u * u + 2.0 * M * u * u * u;
                if (disc < 0.0)
                    disc = 0.0;
                float v = sqrt(disc);
                float vy = (uv.y - 0.5) * 2.2;
                v *= 1.0 + vy * 0.18;

                float phi = 0.0;
                int steps = clamp(_Steps, 24, 128);
                float h = max(_Dphi, 0.012);

                float3 acc = float3(0, 0, 0);
                float dIn = _DiskIn * M;
                float dOut = _DiskOut * M;
                float rPhIn = 2.95 * M;
                float rPhOut = 3.15 * M;

                for (int k = 0; k < 128; k++)
                {
                    if (k >= steps)
                        break;

                    float r = 1.0 / max(u, 1e-5);
                    if (r < Rs + 0.015)
                    {
                        acc += float3(0, 0, 0.015);
                        break;
                    }

                    float x = r * cos(phi);
                    float z = r * sin(phi);
                    float rho = sqrt(x * x + z * z);

                    if (rho > dIn && rho < dOut)
                    {
                        float t = saturate((rho - dIn) / max(dOut - dIn, 1e-3));
                        float band = smoothstep(0.0, 0.08, t) * smoothstep(1.0, 0.92, t);
                        float spin = phi + _TimeLive * _DiskSpin;
                        float dop = 1.0 + 0.42 * sin(spin + uv.x * 6.28318);
                        dop *= 1.0 - 0.22 * sin(spin * 0.5 + vy * 3.14);
                        float3 hot = lerp(float3(1.0, 0.18, 0.02), float3(1.0, 0.55, 0.2), t);
                        hot = lerp(hot, float3(1.0, 0.85, 0.55), pow(t, 2.2));
                        acc += hot * band * dop * _DiskBright * h * 2.8;
                    }

                    if (rho > rPhIn && rho < rPhOut)
                    {
                        float pr = smoothstep(rPhIn, rPhIn + 0.04 * M, rho) * smoothstep(rPhOut, rPhOut - 0.04 * M, rho);
                        acc += float3(1.0, 0.95, 0.78) * pr * _PhotonRing * h * 0.45;
                    }

                    rk4Step(u, v, phi, M, h);

                    if (u < 0.0 || u > 200.0)
                        break;

                    if (u > 1.0 / max(Rs * 0.98, 1e-4))
                        break;
                }

                float2 suv = uv * float2(_Aspect * 120.0, 120.0);
                float stars = pow(n2(floor(suv)), 12.0) * _StarField;
                acc += float3(0.55, 0.62, 1.0) * stars;

                float lum = max(max(acc.x, acc.y), acc.z);
                acc = acc / (lum + 0.85);
                fixed4 outc = fixed4(acc, _Color.a * i.color.a);
                outc.rgb *= _Color.rgb * i.color.rgb;
                return outc;
            }
            ENDCG
        }
    }
    FallBack Off
}
