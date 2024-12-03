Shader "Custom/TransparentTexture"
{
    Properties
    {
        _MainTex ("Base (RGBA)", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        
        CGPROGRAM            
        #pragma surface surf Lambert
        #include "UnityCG.cginc"
            
        sampler2D _MainTex;
            
        struct Input {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
        
    }
    FallBack "Diffuse"
}