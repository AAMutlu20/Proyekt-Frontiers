Shader "TrueGen/TerrainMultiTexture"
{
    Properties
    {
        [Header(Buildable Texture Slot)]
        _Texture0 ("Albedo 0 (Grass)", 2D) = "white" {}
        _Normal0 ("Normal 0", 2D) = "bump" {}
        _Tiling0 ("Tiling 0", Float) = 1
        
        [Header(Path Texture Slot)]
        _Texture1 ("Albedo 1 (Dirt)", 2D) = "white" {}
        _Normal1 ("Normal 1", 2D) = "bump" {}
        _Tiling1 ("Tiling 1", Float) = 1
        
        [Header(Blocked Texture Slot)]
        _Texture2 ("Albedo 2 (Rock)", 2D) = "white" {}
        _Normal2 ("Normal 2", 2D) = "bump" {}
        _Tiling2 ("Tiling 2", Float) = 1
        
        [Header(Decorative Texture Slot)]
        _Texture3 ("Albedo 3 (Variation)", 2D) = "white" {}
        _Normal3 ("Normal 3", 2D) = "bump" {}
        _Tiling3 ("Tiling 3", Float) = 1
        
        [Header(Settings)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1; // Texture index in x component
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float textureIndex : TEXCOORD4;
                float4 color : COLOR;
                float fogFactor : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
            };
            
            // Texture slots
            TEXTURE2D(_Texture0); SAMPLER(sampler_Texture0);
            TEXTURE2D(_Normal0); SAMPLER(sampler_Normal0);
            TEXTURE2D(_Texture1); SAMPLER(sampler_Texture1);
            TEXTURE2D(_Normal1); SAMPLER(sampler_Normal1);
            TEXTURE2D(_Texture2); SAMPLER(sampler_Texture2);
            TEXTURE2D(_Normal2); SAMPLER(sampler_Normal2);
            TEXTURE2D(_Texture3); SAMPLER(sampler_Texture3);
            TEXTURE2D(_Normal3); SAMPLER(sampler_Normal3);
            
            CBUFFER_START(UnityPerMaterial)
                float _Tiling0, _Tiling1, _Tiling2, _Tiling3;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionHCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.textureIndex = input.uv2.x;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample the correct texture based on index
                half4 albedo;
                half3 normal;
                float tiling;
                
                int texIndex = (int)round(input.textureIndex);
                float2 tiledUV = input.uv;
                
                // Select texture based on index (0-3)
                if (texIndex == 0)
                {
                    tiling = _Tiling0;
                    tiledUV *= tiling;
                    albedo = SAMPLE_TEXTURE2D(_Texture0, sampler_Texture0, tiledUV);
                    normal = UnpackNormal(SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, tiledUV));
                }
                else if (texIndex == 1)
                {
                    tiling = _Tiling1;
                    tiledUV *= tiling;
                    albedo = SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, tiledUV);
                    normal = UnpackNormal(SAMPLE_TEXTURE2D(_Normal1, sampler_Normal1, tiledUV));
                }
                else if (texIndex == 2)
                {
                    tiling = _Tiling2;
                    tiledUV *= tiling;
                    albedo = SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, tiledUV);
                    normal = UnpackNormal(SAMPLE_TEXTURE2D(_Normal2, sampler_Normal2, tiledUV));
                }
                else // texIndex == 3
                {
                    tiling = _Tiling3;
                    tiledUV *= tiling;
                    albedo = SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, tiledUV);
                    normal = UnpackNormal(SAMPLE_TEXTURE2D(_Normal3, sampler_Normal3, tiledUV));
                }
                
                // Blend with vertex color for tinting
                albedo.rgb *= input.color.rgb;
                
                // Setup lighting data
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = input.shadowCoord;
                lightingInput.fogCoord = input.fogFactor;
                
                // Transform normal to world space
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                lightingInput.normalWS = normalize(mul(normal, TBN));
                
                // Surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = 1;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normal;
                surfaceData.occlusion = 1;
                
                // Calculate lighting
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, lightingInput.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask R
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}