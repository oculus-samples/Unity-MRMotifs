// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Meta/PassthroughFader"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        _InvertedAlpha("Inverted Alpha", float) = 1
        _FadeDirection("Fade Direction", Int) = 4

        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [HideInInspector] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpColor("Blend Color", Float) = 2
        [HideInInspector] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha("Blend Alpha", Float) = 3
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent-1"
        }
        LOD 100
        Pass
        {
            Cull Off
            ZTest[_ZTest]
            BlendOp[_BlendOpColor], [_BlendOpAlpha]
            Blend Zero One, One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _InvertedAlpha;
            int _FadeDirection;

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 col = tex2D(_MainTex, i.uv);

                float fadeFactor = 1.0f;
                float dissolveThreshold = _InvertedAlpha;
                float edgeWidth = 0.1;
                float2 center = float2(.75, .5);

                const float distance_from_center = length(i.uv - center);

                if (_FadeDirection == 0) {
                    const float alpha = lerp(col.r, 1 - col.r, _InvertedAlpha);
                    col.a *= alpha;
                } else if (_FadeDirection == 1) {
                    fadeFactor = 1.0f - i.uv.x;
                } else if (_FadeDirection == 2) {
                    fadeFactor = 1.0f - i.uv.y;
                } else if (_FadeDirection == 3) {
                    fadeFactor = 1.0f - distance_from_center;
                }

                col.a *= smoothstep(dissolveThreshold - edgeWidth,
                        dissolveThreshold + edgeWidth, fadeFactor);

                return col;
            }
            ENDCG
        }
    }
}
