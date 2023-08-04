Shader "Custom/KeystoneCorrection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TopLeftOffset ("TopLeftOffset", Vector) = (0,0,0,0)
        _TopRightOffset ("TopRightOffset", Vector) = (0,0,0,0)
        _BottomLeftOffset ("BottomLeftOffset", Vector) = (0,0,0,0)
        _BottomRightOffset ("BottomRightOffset", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

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

            sampler2D _MainTex;
            float4 _TopLeftOffset;
            float4 _TopRightOffset;
            float4 _BottomLeftOffset;
            float4 _BottomRightOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Transform UV coordinates to screen space [-1, 1]
                float2 uv = v.uv * 2 - 1;

                // Offset based on screen space coordinates
                if(uv.x < 0) // Left half
                {
                    if(uv.y > 0) // Top half
                    {
                        uv -= _TopLeftOffset.xy;
                    }
                    else // Bottom half
                    {
                        uv -= _BottomLeftOffset.xy;
                    }
                }
                else // Right half
                {
                    if(uv.y > 0) // Top half
                    {
                        uv -= _TopRightOffset.xy;
                    }
                    else // Bottom half
                    {
                        uv -= _BottomRightOffset.xy;
                    }
                }

                // Transform back to UV space [0, 1]
                o.uv = uv * 0.5 + 0.5;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Check if UVs are out of bounds and return a color.
                if (any(i.uv < 0) || any(i.uv > 1)) {
                    return fixed4(0.0, 0.0, 0.0, 1.0); // Change this to your desired out-of-bounds color.
                }
                
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}