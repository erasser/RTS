// https://www.stevestreeting.com/2019/02/22/enemy-health-bars-in-1-draw-call-in-unity/

Shader "UI/HealthBar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}   // The visible texture
        _Fill ("Fill", float) = 0               // Phase [0;1]
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" }            // Render on top
        LOD 100

        Pass
        {
            ZTest Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_fog
            #pragma multi_compile_instancing    // Compile for instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                // UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            UNITY_INSTANCING_BUFFER_START(Props)        // Creates Props buffer
            UNITY_DEFINE_INSTANCED_PROP(float, _Fill)   // Variable per instance
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float fill = UNITY_ACCESS_INSTANCED_PROP(Props, _Fill);

                o.vertex = UnityObjectToClipPos(v.vertex);

                // Generate UVs from fill level (assumed texture is clamped)
                o.uv = v.uv;
                o.uv.x += .5 - fill;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
