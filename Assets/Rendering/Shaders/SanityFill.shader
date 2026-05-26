Shader "UI/SanityFill"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillAmount ("Fill Amount", Range(0,1)) = 1.0
        _GrayStrength ("Gray Strength", Range(0,1)) = 0.85
        _SmoothStep ("Smooth Edge Size", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FillAmount;
            float _GrayStrength;
            float _SmoothStep;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);
                fixed4 tinted = texColor * IN.color;

                // UV.y: 0=bottom, 1=top
                // _FillAmount=1 → full color (healthy), =0 → fully grayed (no sanity)
                // Filled (color) zone is BOTTOM portion up to _FillAmount
                // Grayed zone is TOP portion above _FillAmount
                float uvY = 1.0 - IN.texcoord.y; // flip: now 0=top, 1=bottom
                float threshold = 1.0 - _FillAmount;

                float blendFactor = smoothstep(
                    threshold + _SmoothStep,
                    threshold - _SmoothStep,
                    uvY
                );

                // Grayscale conversion keeping image details visible
                float luminance = dot(tinted.rgb, float3(0.299, 0.587, 0.114));
                // Mix between luminance (gray) and original: _GrayStrength controls how desaturated
                fixed3 grayRgb = lerp(tinted.rgb, fixed3(luminance, luminance, luminance), _GrayStrength);

                fixed4 result;
                result.rgb = lerp(tinted.rgb, grayRgb, blendFactor);
                result.a   = tinted.a;

                return result;
            }
            ENDCG
        }
    }
}