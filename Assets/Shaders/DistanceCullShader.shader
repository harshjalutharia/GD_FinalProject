Shader "Custom/GrassShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MinDistance ("Minimum Distance", float) = 3
        _MaxDistance ("Maximum Distance", float) = 100
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _MinDistance;
        float _MaxDistance;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a));
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Set up the initial color and albedo
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c;
            o.Alpha = 1;

            // calculate the distance between this and the world camera
            float dist = distance(IN.worldPos, _WorldSpaceCameraPos);

            // We want the modification of alpha to be based on the following rules:
            //  1. If the distance is smaller than the min distance, then we set alpha to 1
            //  2. If the distance is larger than the max distance, then we set alpha to 0
            //  3. Otherwise, we need to calculate the falloff based on where the distance lies between min and max dist.
            if (dist > _MinDistance) {
                // Set albedo to just RGB segment of the provided texture
                o.Albedo = c.rgb;
                // Use `inverseLerp` to get the ratio based on min, max, and distance
                float r = 1 - clamp(inverseLerp(_MinDistance, _MaxDistance, dist), 0, 1);
                // Set the alpha
                o.Alpha = r;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
