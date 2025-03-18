// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Meta/DepthLookingGlassMotif"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StartColor ("Start Color", Color) = (0, 0, 1, 1)
        _EndColor ("End Color", Color) = (0, 1, 0, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest-1"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 posWorld : TEXCOORD1;
                float4 grabPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _StartColor;
            float4 _EndColor;

            uniform float4 _CameraDepthTexture_TexelSize;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            Interpolators vert(Attributes v)
            {
                Interpolators o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(Interpolators, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                return o;
            }

            float invLerp(float from, float to, float value)
            {
                return (value - from) / (to - from);
            }

            fixed4 frag(Interpolators i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 screenPosUV = i.grabPos.xy / i.grabPos.w;
                // screenPosUV.y = 1.0f - screenPosUV.y; // uncomment for BiRP
                float virtualLinearDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,screenPosUV));

                const float4 depthSpace = mul(_EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex], float4(i.posWorld, 1.0));
                const float3 uv = float3(depthSpace.xy / depthSpace.w * 0.5f + 0.5f, unity_StereoEyeIndex);

                const float inputDepthEye = _EnvironmentDepthTexture.Sample(sampler_EnvironmentDepthTexture, uv).r;
                const float inputDepthNdc = inputDepthEye * 2.0 - 1.0;
                float envLinearDepth = 1.0f / (inputDepthNdc + _EnvironmentDepthZBufferParams.y) * _EnvironmentDepthZBufferParams.x;
                float linearDepth = min(envLinearDepth, virtualLinearDepth);
                const float remapped = invLerp(0.3, 7.0, linearDepth);

                fixed4 col = lerp(_StartColor, _EndColor, remapped);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
