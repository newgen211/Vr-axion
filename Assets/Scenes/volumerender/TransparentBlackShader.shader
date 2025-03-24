Shader "Custom/DoubleSidedAdjustableOpacity"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Transparency Threshold", Range(0,1)) = 0.1  // Threshold for near-black pixels
        _Opacity ("Opacity Factor", Range(0,1)) = 0.5            // Control overall opacity
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Cull Off  // Disable back-face culling to render both sides
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ColorMask RGB
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

            sampler2D _MainTex;
            float _Threshold;  // Transparency threshold for near-black pixels
            float _Opacity;    // Overall opacity control

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                
                // Compute the brightness of the pixel (average of the RGB values)
                float brightness = (col.r + col.g + col.b) / 3.0;
                
                // If the pixel's brightness is below the threshold, make it almost transparent
                if (brightness < _Threshold)
                {
                    col.a = 0.1 * _Opacity;  // Almost transparent, modifiable by opacity factor
                }
                else
                {
                    col.a = _Opacity;  // Set opacity for non-black pixels
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
