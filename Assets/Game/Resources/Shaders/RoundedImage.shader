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
                
                // 将 UV 映射到 [-0.5, 0.5] 范围，并考虑宽高比
                float2 uv = IN.texcoord - 0.5;
                float2 size = float2(1.0, _AspectRatio);
                float2 p = uv * size;
                
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
                
                // 将半径从“相对于短边的比例”转换为当前坐标系下的绝对值
                radius *= min(1.0, _AspectRatio);
                
                // 确保半径不会超过边长的一半
                radius = min(radius, min(size.x, size.y) * 0.5);
                
                // 计算 Rounded Box SDF
                // q 是点到内部矩形边缘的距离
                float2 q = abs(p) - (size * 0.5) + radius;
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                
                // 抗锯齿：使用 fwidth 获取当前像素的距离变化率
                float blur = fwidth(dist) * 0.5;
                color.a *= smoothstep(blur, -blur, dist);
                
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
