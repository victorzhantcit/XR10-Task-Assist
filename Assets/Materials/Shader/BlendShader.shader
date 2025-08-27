Shader "Custom/BlendShader"
{
    Properties
    {
        _MainTex ("Webcam Texture", 2D) = "white" {}  // �I���AWebCam �e��
        _HologramTex ("Hologram Texture", 2D) = "white" {}  // �e���A������
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

            sampler2D _MainTex;      // WebCam �v��
            sampler2D _HologramTex;  // Hologram �v��

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Ū�� WebCam �e���M Hologram �e��
                fixed4 webCamColor = tex2D(_MainTex, i.uv);
                fixed4 hologramColor = tex2D(_HologramTex, i.uv);
                
                // �V�X�ĪG�A��������Ϫ��z���� (��� hologramColor �� alpha ��)
                return lerp(webCamColor, hologramColor, hologramColor.a);
            }
            ENDCG
        }
    }
}
