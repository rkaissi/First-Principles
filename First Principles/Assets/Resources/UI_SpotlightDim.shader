// Soft fullscreen dim with a transparent elliptical "hole" for uGUI Image (Screen Space Overlay).
// Center is in UV space of the stretched Image (0–1). _Aspect = overlay width / height for a circular hole on screen.
// Lives under Resources/ so runtime Material allocation keeps the shader in builds.
Shader "UI/SpotlightDim"
{
    Properties
    {
        _SpotCenter ("Spot Center UV", Vector) = (0.5, 0.5, 0, 0)
        _SpotRadius ("Hole radius (UV-ish)", Float) = 0.2
        _SpotSoft ("Edge softness", Float) = 0.12
        _Aspect ("Width / height", Float) = 1.7
        _DimColor ("Dim multiply", Color) = (0, 0, 0, 0.82)
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

            float4 _SpotCenter;
            float _SpotRadius;
            float _SpotSoft;
            float _Aspect;
            float4 _DimColor;

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
                float2 d = float2((i.uv.x - _SpotCenter.x) * _Aspect, i.uv.y - _SpotCenter.y);
                float dist = length(d);
                float mask = smoothstep(_SpotRadius - _SpotSoft, _SpotRadius + _SpotSoft, dist);
                float a = mask * _DimColor.a * i.color.a;
                return fixed4(_DimColor.rgb * i.color.rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
