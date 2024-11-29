Shader "Custom/Grass2"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _WindSpeed ("Wind Speed", Range(0.0, 1.0)) = 0.5
        _WindStrength ("Wind Strength", Range(0.0, 1.0)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Properties
            uniform float _WindSpeed;
            uniform float _WindStrength;

            // Vertex Shader
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                //// Simulate wind by distorting UVs based on position and time
                //float wind = sin(v.vertex.x * 0.1 + _Time * _WindSpeed) * _WindStrength;
                o.uv = v.uv; //+ float2(wind, 0.0);
                
                return o;
            }

            // Fragment Shader
            sampler2D _MainTex;
            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}