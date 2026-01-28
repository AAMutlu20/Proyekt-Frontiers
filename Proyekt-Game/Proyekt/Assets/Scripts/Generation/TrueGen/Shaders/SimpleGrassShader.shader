Shader "Custom/SimpleGrassWind"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Wind Settings)]
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.15
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2.0
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.3, 0)
        _WindFrequency ("Wind Frequency", Range(0, 10)) = 3.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Cull Off // Render both sides
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _WindStrength;
                float _WindSpeed;
                float4 _WindDirection;
                float _WindFrequency;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Get world position
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Calculate wind
                float time = _Time.y * _WindSpeed;
                float windWave = sin(time + positionWS.x * _WindFrequency + positionWS.z * _WindFrequency * 0.8);
                float windWave2 = cos(time * 1.5 + positionWS.x * _WindFrequency * 0.6);
                float wind = (windWave + windWave2 * 0.3);
                
                // Height influence (grass is flexible)
                float heightInfluence = saturate(input.positionOS.y + 0.5);
                heightInfluence = heightInfluence * heightInfluence;
                
                // Apply wind
                float3 windOffset = _WindDirection.xyz * wind * _WindStrength * heightInfluence;
                positionWS += windOffset;
                
                // Transform to clip space
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = texColor * _Color;
                
                // Simple lighting
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float NdotL = saturate(dot(input.normalWS, lightDir));
                color.rgb *= NdotL * _MainLightColor.rgb + 0.3; // Ambient
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}