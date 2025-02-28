/************************************************************************************
Copyright (c) Meta Platforms, Inc. and affiliates.
All rights reserved.

Licensed under the Oculus SDK License Agreement (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/

Shader "Meta/PassthroughDissolver"
{
    Properties
    {
        _Level("Dissolution Level", Range(0.0, 1.0)) = 0.0
        _InvertedAlpha("Inverted Alpha", float) = 0
        [HideInInspector]_NoiseTex("Noise Texture", 2D) = "white" {}
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}

        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [HideInInspector] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpColor("Blend Color", Float) = 2
        [HideInInspector] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha("Blend Alpha", Float) = 3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent-1" }
        LOD 100
        Pass
        {
            ZWrite Off
            ZTest[_ZTest]
            BlendOp[_BlendOpColor], [_BlendOpAlpha]
            Blend Zero One, One One
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float _Level;
            float _InvertedAlpha;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex + v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float noiseValue = tex2D(_NoiseTex, i.uv).r;
                fixed4 col = tex2D(_MainTex, i.uv);
                float baseAlpha = smoothstep(_Level, _Level, noiseValue);
                float finalAlpha = lerp(baseAlpha, 1.0 - baseAlpha, _InvertedAlpha);

                if (noiseValue < _Level)
                {
                    return float4(col.rgb, 0.0);
                }
                return float4(0, 0, 0, finalAlpha);
            }
            ENDCG
        }
    }
}
