 /*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

Shader "Meta/DepthRelightingMotif"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        ZTest Always
        ZWrite Off
        Cull Front
        Blend One OneMinusSrcAlpha

        Pass {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
            CBUFFER_END

            Texture2DArray<float> _EnvironmentDepthTexture;
            SamplerState bilinearClampSampler;
            float4 _EnvironmentDepthZBufferParams;
            float4x4 _EnvironmentDepthReprojectionMatrices[2];
            float4x4 _EnvironmentDepthInverseReprojectionMatrices[2];

            float SampleDepthNDC(float2 uv, int eye = 0)
            {
                return _EnvironmentDepthTexture.SampleLevel(bilinearClampSampler, float3(uv.xy, eye), 0).r;
            }

            float3 WorldtoNDC(float3 worldPos, int eye = 0)
            {
                float4 hcs = mul(_EnvironmentDepthReprojectionMatrices[eye], float4(worldPos, 1));
                return (hcs.xyz / hcs.w) * 0.5 + 0.5;
            }

            float3 NDCtoWorld(float3 ndc, int eye = 0)
            {
                float4 hcs = float4(ndc * 2.0 - 1.0, 1);
                float4 worldH = mul(_EnvironmentDepthInverseReprojectionMatrices[eye], hcs);
                return worldH.xyz / worldH.w;
            }

            float SampleEnvironmentDepthLinear(float2 uv)
            {
                float inputDepthEye = SampleDepthNDC(uv);
                float inputDepthNdc = inputDepthEye * 2.0 - 1.0;
                float linearDepth = (1.0f / (inputDepthNdc + _EnvironmentDepthZBufferParams.y)) * _EnvironmentDepthZBufferParams.x;
                return linearDepth;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 positionHCSTexCoord : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionHCSTexCoord = OUT.positionHCS;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                int eye = unity_StereoEyeIndex;

                // Compute NDC (Normalized Device Coordinates)
                float3 ndc = WorldtoNDC(IN.positionWS, eye);

                // Sample linear depth from the environment depth texture
                float depthNDC = SampleDepthNDC(ndc.xy, eye);

                // Convert NDC to World coordinates using precomputed inverse matrices
                float3 depthWorld = NDCtoWorld(float3(ndc.xy, depthNDC), eye);

                // Light calculations
                float3 lightPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 diff = lightPos - depthWorld;
                float3 lightDir = normalize(diff);

                // Calculate surface normal using finite differences
                float2 uvH = ndc.xy + float2(0.001, 0);
                float2 uvV = ndc.xy + float2(0, 0.001);
                float3 depthWorldH = NDCtoWorld(float3(uvH, SampleDepthNDC(uvH, eye)), eye);
                float3 depthWorldV = NDCtoWorld(float3(uvV, SampleDepthNDC(uvV, eye)), eye);

                float3 hDeriv = depthWorldH - depthWorld;
                float3 vDeriv = depthWorldV - depthWorld;
                float3 worldNorm = -normalize(cross(hDeriv, vDeriv));

                // Calculate intensity based on normal and light direction
                float dist = length(diff);
                float rad = length(mul(unity_ObjectToWorld, float4(1, 0, 0, 0))) / 2;
                float intensity = max(dot(worldNorm, lightDir), 0.0) * pow(max(0, 1 - dist / rad), 2) * _Intensity;

                // Output color
                return float4(_Color.rgb * intensity, 0);
            }

            ENDHLSL
        }
    }
}
