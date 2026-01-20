Shader "TrueGen/GridOverlay"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (1,1,1,0.3)
        _LineWidth ("Line Width", Range(0.01, 0.2)) = 0.05
        _GridSize ("Grid Size", Float) = 10.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "GridOverlay"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float _LineWidth;
                float _GridSize;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Calculate grid lines based on world position
                float2 gridPos = input.worldPos.xz / _GridSize;
                float2 grid = abs(frac(gridPos - 0.5) - 0.5) / fwidth(gridPos);
                float gridLine = min(grid.x, grid.y);
                
                // Create sharp grid lines
                float lineIntensity = 1.0 - min(gridLine / _LineWidth, 1.0);
                
                // Mix grid color with chunk color
                float4 finalColor = lerp(float4(0,0,0,0), _GridColor, lineIntensity);
                
                // Add slight tint based on vertex color (buildable/non-buildable)
                finalColor.rgb = lerp(finalColor.rgb, input.color.rgb, 0.2);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}