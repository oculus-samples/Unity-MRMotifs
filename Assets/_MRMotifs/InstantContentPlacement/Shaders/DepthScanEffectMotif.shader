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

Shader "Meta/DepthScanEffectMotif"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 1, 1)
        _Girth ("Girth", float) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        Cull Off
        ZWRITE Off
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "UnityCG.cginc"
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
                float4 vertex : SV_POSITION;
                float3 posWorld : TEXCOORD1;
                float4 color : COLOR0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Girth;
            v2f vert (appdata v)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(v.vertex.xyz);

                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(output, v.vertex)
                output.color = v.color;

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                return output;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = _Color;
                col *= i.color;

                float virtualSceneDepth = length(i.posWorld - _WorldSpaceCameraPos);

                const float4 depthSpace =
                mul(_EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex], float4(i.posWorld, 1.0));
                const float2 uvCoords = (depthSpace.xy / depthSpace.w + 1.0f) * 0.5f;
                float environmentDepth = SampleEnvironmentDepthLinear(uvCoords);

                col *= (1 - (clamp(abs(environmentDepth - virtualSceneDepth), 0.0f, _Girth) / _Girth));

                 if(abs(environmentDepth - virtualSceneDepth) > _Girth)
                     discard;

                return col;
            }
            ENDHLSL
        }
    }
}
