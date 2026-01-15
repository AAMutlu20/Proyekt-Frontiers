Shader "Hidden/LowPolyEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 100
        
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "LowPolyEffect"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            
            float _ColorSteps;
            float _Pixelation;
            float _EdgeThickness;
            float4 _EdgeColor;
            float _EdgeIntensity;
            
            // Posterize colors
            float3 Posterize(float3 color, float steps)
            {
                return floor(color * steps) / steps;
            }
            
            // Detect edges using Sobel filter
            float SobelEdgeDetection(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy * _EdgeThickness;
                
                // Sobel kernels
                float3 sample00 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, -texel.y)).rgb;
                float3 sample01 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -texel.y)).rgb;
                float3 sample02 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, -texel.y)).rgb;
                
                float3 sample10 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, 0)).rgb;
                float3 sample12 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, 0)).rgb;
                
                float3 sample20 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, texel.y)).rgb;
                float3 sample21 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texel.y)).rgb;
                float3 sample22 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, texel.y)).rgb;
                
                // Sobel operator
                float3 gx = -sample00 - 2.0 * sample10 - sample20 + sample02 + 2.0 * sample12 + sample22;
                float3 gy = -sample00 - 2.0 * sample01 - sample02 + sample20 + 2.0 * sample21 + sample22;
                
                float edge = length(gx) + length(gy);
                return saturate(edge);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // Pixelation effect
                if (_Pixelation > 1.0)
                {
                    float2 pixelSize = _MainTex_TexelSize.xy * _Pixelation;
                    uv = floor(uv / pixelSize) * pixelSize;
                }
                
                // Sample texture
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Posterize colors
                color.rgb = Posterize(color.rgb, _ColorSteps);
                
                // Edge detection
                if (_EdgeIntensity > 0.0)
                {
                    float edge = SobelEdgeDetection(input.texcoord);
                    color.rgb = lerp(color.rgb, _EdgeColor.rgb, edge * _EdgeIntensity);
                }
                
                return color;
            }
            ENDHLSL
        }
    }
}