Shader "UI/LiquidFillMaskable"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _FillAmount ("Fill Amount", Range(0,1)) = 0.5

        _WaveAmplitude ("Wave Amplitude", Range(0,0.05)) = 0.02
        _WaveFrequency ("Wave Frequency", Range(0,20)) = 8
        _WaveSpeed ("Wave Speed", Range(0,5)) = 1

        _LiquidColor ("Liquid Color", Color) = (0,0.6,1,1)

        // UI Mask properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;

            float _FillAmount;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;

            float4 _LiquidColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float wave =
                    sin((uv.x * _WaveFrequency) + (_Time.y * _WaveSpeed))
                    * _WaveAmplitude;

                float level = _FillAmount + wave;

                if (uv.y > level)
                    discard;

                fixed4 col = _LiquidColor;
                col *= i.color;

                return col;
            }

            ENDCG
        }
    }
}