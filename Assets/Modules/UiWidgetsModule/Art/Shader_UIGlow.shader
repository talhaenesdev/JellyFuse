// A soft additive halo for UI: what a bloom would spread around something bright, drawn as one quad behind it.
// It breathes on its own clock (_Pulse cycles a second, _PulseDepth of the alpha), so no script ticks it, and
// the global _UIGlowStrength - 1 or 0, set by MainModule from the frame rate - lights or hides every halo at once.
Shader "UI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Halo", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.85, 0.35, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.1
        _Pulse ("Pulses a second", Range(0, 4)) = 0.6
        _PulseDepth ("Pulse depth", Range(0, 1)) = 0.35
        _Phase ("Phase", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // Alpha blended rather than additive: the menus are light, and adding light to near-white shows nothing.
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Intensity, _Pulse, _PulseDepth, _Phase;
            float4 _ClipRect;
            float _UIGlowStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float wave = 0.5 + 0.5 * sin((_Time.y * _Pulse + _Phase) * 6.2831853);
                c.a *= _Intensity * (1 - _PulseDepth * wave) * _UIGlowStrength;
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                return c;
            }
            ENDCG
        }
    }
}
