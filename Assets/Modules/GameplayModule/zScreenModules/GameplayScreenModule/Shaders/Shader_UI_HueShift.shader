// A UI picture painted in many shades of one colour, turned to the Image's colour: every pixel's hue is moved by
// the step from _Reference to the Image's colour, and its saturation and brightness scaled by the same ratio, so a
// door painted in light and dark blues keeps its light and dark parts in whatever colour it is given.
Shader "Jelly Fuse/UI Hue Shift"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Reference ("The colour the picture is painted in", Color) = (0, 0.559, 1, 1)
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _Reference;
            float4 _ClipRect;

            float3 ToHsv(float3 c)
            {
                float4 k = float4(0, -1.0 / 3.0, 2.0 / 3.0, -1);
                float4 p = lerp(float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                return float3(abs(q.z + (q.w - q.y) / (6 * d + 1e-5)), d / (q.x + 1e-5), q.x);
            }

            float3 ToRgb(float3 c)
            {
                float3 p = abs(frac(c.xxx + float3(1, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3);
                return c.z * lerp(float3(1, 1, 1), saturate(p - 1), c.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float3 from = ToHsv(_Reference.rgb), to = ToHsv(i.color.rgb), hsv = ToHsv(tex.rgb);
                hsv.x = frac(hsv.x + to.x - from.x + 1);
                hsv.y = saturate(hsv.y * to.y / max(from.y, 1e-3));
                hsv.z = saturate(hsv.z * to.z / max(from.z, 1e-3));
                fixed4 colour = fixed4(ToRgb(hsv), tex.a * i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                colour.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return colour;
            }
            ENDCG
        }
    }
}
