// =====================================================================
//  SandDissolve/SpriteDissolve
//  Sand-style dissolve shader for SpriteRenderer and UI Image.
//  Works with URP (2D Renderer and Forward) and Built-in as a fallback.
//  Plain HLSL, no ShaderGraph dependency.
//
//  v1.1 FIX (build error on gles3/vulkan):
//  - Pass "SandDissolveUnlit" (URP): UnityPixelSnap() is a Built-in-only
//    helper from UnitySprites.cginc and does NOT exist in URP's Core.hlsl.
//    A local equivalent using GetScaledScreenParams() is now provided.
//  v1.2 FIX (build error on d3d11, SubShader 1):
//  - Built-in fallback pass: UnitySprites.cginc ALSO declares appdata_t and
//    sampler2D _MainTex, which collide with this pass's own declarations
//    ("redefinition of 'appdata_t'"). The include is REMOVED and the pass
//    now provides its own local UnityPixelSnap() built on _ScreenParams,
//    identical to the official implementation.
//  v1.3 HARDENING: built-in pass structs renamed to DissolveAppData /
//  DissolveVaryings so no Unity cginc (UnitySprites.cginc, UnityUI.cginc, ...)
//  can ever trigger a redefinition, regardless of includes.
//  v1.4 FIX (build error on d3d11, SubShader 1): in this Unity version
//  UnityCG.cginc's include chain ALREADY declares UnityPixelSnap, so the
//  local copy added in v1.2 collided ("redefinition of 'UnityPixelSnap'").
//  The local copy is removed; the fallback now uses the official one.
// =====================================================================

Shader "SandDissolve/SpriteDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color        ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve)]
        _NoiseTex     ("Noise (R)", 2D) = "gray" {}
        _NoiseScale   ("Noise Scale", Float) = 1.0
        _Dissolve     ("Dissolve Amount", Range(0,1)) = 0.0
        _EdgeWidth    ("Edge Width", Range(0.0001,0.5)) = 0.08
        [HDR] _EdgeColor ("Edge Color", Color) = (1.0, 0.82, 0.45, 1)
        _EdgeEmission ("Edge Emission", Range(0,8)) = 1.6

        [Header(Grayscale)]
        [Toggle(_GRAYSCALE_ON)] _Grayscale ("Grayscale", Float) = 0
        _GrayscaleAmount ("Grayscale Amount", Range(0,1)) = 1.0
        _GrayscaleTint   ("Grayscale Tint", Color) = (1,1,1,1)

        [Header(Optional direction)]
        // (x,y) = sweep direction in UV space. (0,0) = uniform dissolve.
        _DirX         ("Direction X", Range(-1,1)) = 0
        _DirY         ("Direction Y", Range(-1,1)) = 0
        _DirStrength  ("Direction Strength", Range(0,1)) = 0.0
        // 0 = directional (uses _DirX/_DirY), 1 = center out, 2 = edges in
        _PatternMode  ("Pattern Mode", Float) = 0

        [Header(Grain)]
        _GrainScale   ("Grain Scale", Float) = 90.0
        _GrainAmount  ("Grain Amount", Range(0,0.5)) = 0.12

        [Header(Sprite settings)]
        [Toggle(PIXELSNAP_ON)] PixelSnap ("Pixel snap", Float) = 0

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)

        // Required so the shader behaves inside a UI Image (Mask / RectMask2D)
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
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

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
        Blend One OneMinusSrcAlpha      // premultiplied (URP sprite standard)
        ColorMask [_ColorMask]

        Pass
        {
            Name "SandDissolveUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            // Optional fine grain: disable on low-end to save ALU.
            // multi_compile (not shader_feature) because the material is created at
            // runtime: shader_feature variants get stripped from builds when no
            // material asset has the keyword enabled.
            #pragma multi_compile_local_fragment _ _HFGRAIN_ON
            // Grayscale is a variant, so it costs literally nothing when off.
            #pragma multi_compile_local_fragment _ _GRAYSCALE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4  _Color;
                half4  _EdgeColor;
                half4  _GrayscaleTint;
                float  _NoiseScale;
                float  _Dissolve;
                float  _EdgeWidth;
                float  _EdgeEmission;
                float  _GrayscaleAmount;
                float  _DirX;
                float  _DirY;
                float  _DirStrength;
                float  _PatternMode;
                float  _GrainScale;
                float  _GrainAmount;
            CBUFFER_END

            // Injected by the SpriteRenderer, must stay outside the CBUFFER.
            half4 _RendererColor;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // [FIX] UnityPixelSnap is a Built-in helper (UnitySprites.cginc)
            // and is missing from URP's Core.hlsl. Same behavior implemented
            // with GetScaledScreenParams() (part of URP's Core.hlsl).
            float4 UnityPixelSnap(float4 pos)
            {
                float2 hpc = GetScaledScreenParams().xy * 0.5f;
                float2 pixelPos = round((pos.xy / pos.w) * hpc);
                pos.xy = pixelPos / hpc * pos.w;
                return pos;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.positionCS = UnityPixelSnap(OUT.positionCS);
                #endif

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // ---- Grayscale ---------------------------------------------
                // Applied before the edge tint so the glowing rim keeps its color.
                #ifdef _GRAYSCALE_ON
                    half luma = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                    half3 gray = luma * _GrayscaleTint.rgb;
                    tex.rgb = lerp(tex.rgb, gray, _GrayscaleAmount);
                #endif

                // ---- Noise mask --------------------------------------------
                float2 nuv   = IN.uv * _NoiseScale;
                float  noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv).r;

                // high frequency grain -> sandy look
                #ifdef _HFGRAIN_ON
                    float grain = hash21(floor(IN.uv * _GrainScale));
                    noise = saturate(noise + (grain - 0.5) * _GrainAmount * 2.0);
                #endif

                // optional pattern bias (sweep / radial)
                UNITY_BRANCH
                if (_DirStrength > 0.001)
                {
                    float proj;
                    UNITY_BRANCH
                    if (_PatternMode > 0.5)
                    {
                        // radial: normalized so the corners reach ~1
                        float2 d = IN.uv - 0.5;
                        float dist = length(d) * 1.41421356;
                        // low proj dissolves first
                        proj = (_PatternMode > 1.5) ? (1.0 - dist) : dist;
                    }
                    else
                    {
                        proj = dot(IN.uv - 0.5, float2(_DirX, _DirY)) + 0.5;
                    }

                    // Cross-fade around 0.5: the gradient IS the mask, the noise
                    // only jitters the cut line. strength 1 -> straight edge,
                    // strength 0 -> pure noise. Must match SampleNoise() in C#.
                    noise = saturate(0.5 + (proj - 0.5) * _DirStrength
                                         + (noise - 0.5) * (1.0 - _DirStrength));
                }

                // ---- Soft threshold ----------------------------------------
                // Remapped so 0 -> nothing dissolved, 1 -> fully gone.
                float edge   = _EdgeWidth;
                float cut    = _Dissolve * (1.0 + edge * 2.0) - edge;
                float alphaM = smoothstep(cut, cut + edge, noise);

                // ---- Glowing rim -------------------------------------------
                float edgeMask = saturate((1.0 - alphaM) * step(cut, noise));
                edgeMask = pow(edgeMask, 0.6) * step(0.0001, _Dissolve);

                half3 rgb = tex.rgb;
                half  a   = tex.a * alphaM;

                rgb = lerp(rgb, _EdgeColor.rgb * _EdgeEmission, edgeMask * _EdgeColor.a);
                a   = max(a, tex.a * edgeMask * _EdgeColor.a);

                // Premultiplied blend
                return half4(rgb * a, a);
            }
            ENDHLSL
        }
    }

    // ---------------- Built-in Render Pipeline fallback ----------------
    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile PIXELSNAP_ON
            #pragma multi_compile_local _ _HFGRAIN_ON
            #pragma multi_compile_local _ _GRAYSCALE_ON

            #include "UnityCG.cginc"
            // [FIX v1.2] Do NOT include UnitySprites.cginc here: it re-declares
            // appdata_t and sampler2D _MainTex, colliding with the declarations
            // below. Instead, this pass provides its own local UnityPixelSnap().

            struct DissolveAppData { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct DissolveVaryings { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };

            sampler2D _MainTex, _NoiseTex;
            float4 _MainTex_ST;
            fixed4 _Color, _EdgeColor, _RendererColor, _GrayscaleTint;
            float _NoiseScale, _Dissolve, _EdgeWidth, _EdgeEmission, _GrayscaleAmount;
            float _DirX, _DirY, _DirStrength, _PatternMode, _GrainScale, _GrainAmount;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // [FIX v1.4] No local UnityPixelSnap() here: in this Unity version
            // the function is ALREADY declared by UnityCG.cginc's include
            // chain, so a local copy triggers "redefinition". If this shader
            // is ever used on an OLDER Unity that errors with
            // 'undeclared identifier UnityPixelSnap', re-add the local
            // function (see LEEME_ShaderFix.md).

            DissolveVaryings vert(DissolveAppData IN)
            {
                DissolveVaryings OUT;
                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color    = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(DissolveVaryings IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord) * IN.color;

                #ifdef _GRAYSCALE_ON
                    fixed luma = dot(tex.rgb, fixed3(0.299, 0.587, 0.114));
                    fixed3 gray = luma * _GrayscaleTint.rgb;
                    tex.rgb = lerp(tex.rgb, gray, _GrayscaleAmount);
                #endif

                float noise = tex2D(_NoiseTex, IN.texcoord * _NoiseScale).r;

                #ifdef _HFGRAIN_ON
                    float grain = hash21(floor(IN.texcoord * _GrainScale));
                    noise = saturate(noise + (grain - 0.5) * _GrainAmount * 2.0);
                #endif

                float proj;
                if (_PatternMode > 0.5)
                {
                    float2 dd = IN.texcoord - 0.5;
                    float dist = length(dd) * 1.41421356;
                    proj = (_PatternMode > 1.5) ? (1.0 - dist) : dist;
                }
                else
                {
                    proj = dot(IN.texcoord - 0.5, float2(_DirX, _DirY)) + 0.5;
                }
                noise = saturate(0.5 + (proj - 0.5) * _DirStrength
                                     + (noise - 0.5) * (1.0 - _DirStrength));

                float edge   = _EdgeWidth;
                float cut    = _Dissolve * (1.0 + edge * 2.0) - edge;
                float alphaM = smoothstep(cut, cut + edge, noise);

                float edgeMask = saturate((1.0 - alphaM) * step(cut, noise));
                edgeMask = pow(edgeMask, 0.6) * step(0.0001, _Dissolve);

                fixed3 rgb = tex.rgb;
                fixed  a   = tex.a * alphaM;

                rgb = lerp(rgb, _EdgeColor.rgb * _EdgeEmission, edgeMask * _EdgeColor.a);
                a   = max(a, tex.a * edgeMask * _EdgeColor.a);

                return fixed4(rgb * a, a);
            }
            ENDCG
        }
    }
}
