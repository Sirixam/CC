/*Please do support www.bitshiftprogrammer.com by joining the facebook page : fb.com/BitshiftProgrammer
Legal Stuff:
This code is free to use no restrictions but attribution would be appreciated.
Any damage caused either partly or completly due to usage this stuff is not my responsibility*/
Shader "Custom/BillboardUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
	_Scaling("Scaling", Float) = 1.0
	[Toggle] _KeepConstantScaling("Keep Constant Scaling", Float) = 1
	[Enum(RenderOnTop, 0,RenderWithTest, 4)] _ZTest("Render on top", Int) = 1
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _HorizontalCorrection ("Horizontal Correction", Float) = 0.5
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        ZTest [_ZTest]
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

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

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
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
	        float _KeepConstantScaling;
	        float _Scaling;
            float _HorizontalCorrection;

            sampler2D _MainTex;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 viewOrigin = mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0));

                // Perspective divide approximation (before projection)
                float xOffsetFromCenter = viewOrigin.x / viewOrigin.z;

                // Push toward screen center (invert sign)
                float correction = -xOffsetFromCenter * _HorizontalCorrection;

                // Apply correction in view space (camera right = +X in view space)
                viewOrigin.x += correction * viewOrigin.z;

                // Existing scaling
                float relativeScaler = (_KeepConstantScaling) ? distance(mul(unity_ObjectToWorld, v.vertex), _WorldSpaceCameraPos) : 1;

                // Build billboard quad
                float4 billboardOffset = float4(v.vertex.x, v.vertex.y, 0.0, 0.0) * relativeScaler * _Scaling;

                OUT.vertex = mul(UNITY_MATRIX_P, viewOrigin + billboardOffset);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                OUT.worldPosition = worldPos;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                return color;
            }
        ENDCG
        }
    }
}