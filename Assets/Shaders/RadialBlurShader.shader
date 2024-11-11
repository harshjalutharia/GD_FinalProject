Shader "Unlit/RadialBlurShader"
{
    Properties
    {
        _MainTex ("Map Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float _BlurStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the mask texture
                float maskValue = tex2D(_MaskTex, i.uv).r;

                // Determine blur amount based on mask
                float blurAmount = 1.0 - maskValue; // Areas visited have maskValue = 1

                // Sample blurred color by averaging nearby pixels
                float2 offset = float2(_BlurStrength, 0);
                float2 offsets[4] = { offset, -offset, float2(0, _BlurStrength), float2(0, -_BlurStrength) };
                fixed4 blurredColor = 0;
                for (int k = 0; k < 4; k++)
                {
                    blurredColor += tex2D(_MainTex, i.uv + offsets[k]);
                }
                blurredColor /= 4;

                // Sample the clear color
                fixed4 clearColor = tex2D(_MainTex, i.uv);

                // Lerp between clear and blurred color based on blurAmount
                return lerp(clearColor, blurredColor, blurAmount);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
