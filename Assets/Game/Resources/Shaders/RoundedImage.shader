Shader "UI/RoundedImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 四个角的圆角半径（0-0.5，相对于短边的比例）
        _RadiusTL ("Top Left Radius", Range(0, 0.5)) = 0.1
        _RadiusTR ("Top Right Radius", Range(0, 0.5)) = 0.1
        _RadiusBR ("Bottom Right Radius", Range(0, 0.5)) = 0.1
        _RadiusBL ("Bottom Left Radius", Range(0, 0.5)) = 0.1
        
        // UI 元素宽高比（由脚本设置）
        _AspectRatio ("Aspect Ratio", Float) = 1
        
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _RadiusTL;
            float _RadiusTR;
            float _RadiusBR;
            float _RadiusBL;
            float _AspectRatio;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 将 UV 等价到左下角（利用对称性）
                float2 p = abs(step(0.5, IN.texcoord) - IN.texcoord);
                
                // 根据象限获取对应的圆角半径
                float radius;
                if (IN.texcoord.x < 0.5 && IN.texcoord.y > 0.5)      // 左上
                    radius = _RadiusTL;
                else if (IN.texcoord.x > 0.5 && IN.texcoord.y > 0.5) // 右上
                    radius = _RadiusTR;
                else if (IN.texcoord.x > 0.5 && IN.texcoord.y < 0.5) // 右下
                    radius = _RadiusBR;
                else                                                  // 左下
                    radius = _RadiusBL;
                
                // 三个条件同时成立则乘0，否则乘1
                // 1. 在左下角区域内，2. 长度超过半径
                // step(radius, p.x) || step(radius, p.y * _AspectRatio) || step(length(...), radius)
                float keep = step(radius, p.x) + step(radius, p.y * _AspectRatio) + step(length(float2(p.x - radius, p.y * _AspectRatio - radius)), radius);
                color.a *= step(0.5, keep);
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
