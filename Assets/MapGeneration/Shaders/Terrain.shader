Shader "Custom/Terrain"
{
    Properties {
        _GrassTexture ("Grass Texture Height", 2D) = "white" {}
        _ExtrusionWeight ("Extrusion Weight", Range(0,1)) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vert:vertex

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 0.0001;

        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseStartHeights[maxLayerCount]; 
        float baseBlends[maxLayerCount];
        float baseColorStrength[maxLayerCount];
        float baseTextureScales[maxLayerCount];
        
        float minHeight;
        float maxHeight;

        sampler2D _GrassTexture;
        float _ExtrusionWeight;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };
        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a));
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;
            
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        
        void vert (inout appdata_full v) {
            float noise = tex2Dlod(_GrassTexture, float4(v.texcoord.xy, 0.0, 0.0)).r; // Use the red channel of the texture for extrusion
            float extrusion = noise * _ExtrusionWeight;
            v.vertex.xyz += v.normal * extrusion;
        }
        

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            for(int i = 0; i < layerCount; i++) {
                float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
                
                float3 baseColor = baseColors[i] * baseColorStrength[i];
                float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1-baseColorStrength[i]);

                o.Albedo = o.Albedo * (1-drawStrength) + (baseColor + textureColor) * drawStrength;
                //o.Albedo = o.Albedo * (1-drawStrength) + baseColors[i] * drawStrength;
            }
        }

        ENDCG
    }
    FallBack "Diffuse"
}
