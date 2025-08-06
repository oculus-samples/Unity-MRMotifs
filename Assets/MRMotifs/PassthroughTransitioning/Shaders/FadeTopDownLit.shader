Shader "Custom/FadeTopDownLit"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _FadeAmount("Fade Amount", Range(0,1)) = 1
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _FadeAmount;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float objectHeight = 1.0; // Adjust this for different object heights, or make it a property.
            float heightLerp = saturate((IN.worldPos.y - (_WorldSpaceCameraPos.y - objectHeight)) / objectHeight);
            float fade = saturate(heightLerp / _FadeAmount);

            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = fade * _Color.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
