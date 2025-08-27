Shader "Custom/BlendShader"
{
    Properties
    {
        _MainTex ("Webcam Texture", 2D) = "white" {}  // 背景，WebCam 畫面
        _HologramTex ("Hologram Texture", 2D) = "white" {}  // 前景，全息圖
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;      // WebCam 影像
            sampler2D _HologramTex;  // Hologram 影像

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 讀取 WebCam 畫面和 Hologram 畫面
                fixed4 webCamColor = tex2D(_MainTex, i.uv);
                fixed4 hologramColor = tex2D(_HologramTex, i.uv);
                
                // 混合效果，控制全息圖的透明度 (基於 hologramColor 的 alpha 值)
                return lerp(webCamColor, hologramColor, hologramColor.a);
            }
            ENDCG
        }
    }
}
