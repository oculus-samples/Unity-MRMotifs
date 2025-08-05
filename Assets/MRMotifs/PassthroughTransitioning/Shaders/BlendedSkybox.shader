Shader "Custom/BlendedSkybox"
{
    Properties
    {
        _SkyboxA("Skybox A", CUBE) = "" {}
        _SkyboxB("Skybox B", CUBE) = "" {}
        _BlendAmount("Blend Amount", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _SkyboxA;
            samplerCUBE _SkyboxB;
            float _BlendAmount;

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.texcoord);
                fixed4 colA = texCUBE(_SkyboxA, viewDir);
                fixed4 colB = texCUBE(_SkyboxB, viewDir);
                return lerp(colA, colB, _BlendAmount);
            }
            ENDCG
        }
    }
    FallBack "RenderFX/Skybox"
}
