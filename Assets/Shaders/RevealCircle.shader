Shader "Unlit/RevealCircle"
{
    Properties
    {
        _PrevMask ("Previous Mask", 2D) = "white" {}
        _PlayerPos ("Player Position", Vector) = (0,0,0,0)
        _Radius ("Reveal Radius", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _PrevMask;
            float4 _PlayerPos;
            float _Radius;

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float existingMask = tex2D(_PrevMask, uv).r;

                float dist = distance(uv, _PlayerPos.xy);
                float newMask = smoothstep(_Radius, _Radius * 0.9, dist);

                float combinedMask = max(existingMask, 1.0 - newMask);

                return fixed4(combinedMask, combinedMask, combinedMask, 1.0);
            }
            ENDCG
        }
    }
}
