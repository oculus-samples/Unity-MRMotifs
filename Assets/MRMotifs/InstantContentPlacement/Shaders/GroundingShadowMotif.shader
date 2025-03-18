// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Meta/GroundingShadowMotif"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _FadeStart ("Fade Start", Float) = 0.1
        _FadeRange ("Fade Range", Float) = 0.05
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 posWorld : TEXCOORD1;
                float3 posView : TEXCOORD2;
                float4 color : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FadeStart;
            float _FadeRange;

            v2f vert (appdata v)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(v.uv, _MainTex);
                output.positionCS = TransformObjectToHClip(v.vertex.xyz);
                output.posWorld = TransformObjectToWorld(v.vertex.xyz);
                output.posView = mul(UNITY_MATRIX_MV, v.vertex).xyz;

                output.color = v.color;

                return output;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Sample the texture and apply tint color
                float4 spriteColor = tex2D(_MainTex, i.uv) * _Color * i.color;

                // Apply clipping and fading effect using depth information
                float sceneDepth = -i.posView.z;
                float4 depthSpace = mul(_EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex], float4(i.posWorld, 1.0));
                float2 uvCoords = (depthSpace.xy / depthSpace.w + 1.0f) * 0.5f;
                float environmentDepth = SampleEnvironmentDepthLinear(uvCoords);
                float depthDifference = abs(environmentDepth - sceneDepth);

                // Calculate fade based on depth difference
                float alpha = saturate((_FadeStart + _FadeRange - depthDifference) / _FadeRange);

                // Adjust the alpha of the sprite
                spriteColor.a *= alpha;

                // Discard if alpha is zero to create clipping effect
                if (spriteColor.a <= 0.0f)
                    discard;

                return spriteColor;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 posWorld : TEXCOORD1;
                float3 posView : TEXCOORD2;
                float4 color : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FadeStart;
            float _FadeRange;

            v2f vert (appdata v)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(v.uv, _MainTex);
                output.positionCS = UnityObjectToClipPos(v.vertex.xyz);
                output.posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
                output.posView = mul(UNITY_MATRIX_MV, v.vertex).xyz;

                output.color = v.color;

                return output;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Sample the texture and apply tint color
                float4 spriteColor = tex2D(_MainTex, i.uv) * _Color * i.color;

                // Apply clipping and fading effect using depth information
                float sceneDepth = -i.posView.z;
                float4 depthSpace = mul(_EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex], float4(i.posWorld, 1.0));
                float2 uvCoords = (depthSpace.xy / depthSpace.w + 1.0f) * 0.5f;
                float environmentDepth = SampleEnvironmentDepthLinear(uvCoords);
                float depthDifference = abs(environmentDepth - sceneDepth);

                // Calculate fade based on depth difference
                float alpha = saturate((_FadeStart + _FadeRange - depthDifference) / _FadeRange);

                // Adjust the alpha of the sprite
                spriteColor.a *= alpha;

                // Discard if alpha is zero to create clipping effect
                if (spriteColor.a <= 0.0f)
                    discard;

                return spriteColor;
            }
            ENDCG
        }
    }
}
