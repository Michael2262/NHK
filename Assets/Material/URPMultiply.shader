Shader "Custom/URP_Multiply"
{
    Properties
    {
        // _MainTex 是讓 Sprite Renderer 能把圖片傳進來的關鍵名稱
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
        }

        // 這是色彩增值的核心：將輸出的顏色與背景顏色相乘
        Blend DstColor Zero
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;

            Varyings vert(Attributes IN) {
                Varyings OUT;
                // 將模型空間座標轉換到裁剪空間
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                // 採樣圖片顏色並乘上 Sprite 的 Tint 顏色
                half4 texColor = tex2D(_MainTex, IN.uv);
                return texColor * IN.color;
            }
            ENDHLSL
        }
    }
}