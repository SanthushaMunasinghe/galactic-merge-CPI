// =====================================================================
//  SandDissolve/GrainParticle
//  Minimal shader for the sand grains. No lighting, no fog, no heavy
//  instancing: just texture x vertex color.
//  Built for mobile GPUs (low-end Mali/Adreno).
// =====================================================================
Shader "SandDissolve/GrainParticle"
{
    Properties
    {
        _MainTex ("Grain Texture", 2D) = "white" {}
        [Toggle(_SOFTDOT_ON)] _SoftDot ("Soft dot (no texture)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            Name "GrainUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            // CAMBIO AQUÍ: Asegura que la variante procedural se compile en la build final
            #pragma multi_compile_local _ _SOFTDOT_ON

            // No fog, no lighting: these are 2px specks.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                #ifdef _SOFTDOT_ON
                    // Procedural round dot: zero texture samples.
                    float2 d = IN.uv * 2.0 - 1.0;
                    half a = saturate(1.0 - dot(d, d));
                    a *= a;
                    return half4(IN.color.rgb, IN.color.a * a);
                #else
                    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                    return half4(IN.color.rgb * tex.rgb, IN.color.a * tex.a);
                #endif
            }
            ENDHLSL
        }
    }

    // ---- Built-in fallback ----
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // CAMBIO AQUÍ: También en el Fallback para Built-in Pipeline
            #pragma multi_compile_local _ _SOFTDOT_ON
            
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex:POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; };

            sampler2D _MainTex; float4 _MainTex_ST;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                #ifdef _SOFTDOT_ON
                    float2 d = IN.texcoord * 2.0 - 1.0;
                    fixed a = saturate(1.0 - dot(d, d)); a *= a;
                    return fixed4(IN.color.rgb, IN.color.a * a);
                #else
                    fixed4 tex = tex2D(_MainTex, IN.texcoord);
                    return fixed4(IN.color.rgb * tex.rgb, IN.color.a * tex.a);
                #endif
            }
            ENDCG
        }
    }
}