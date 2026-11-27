Shader "MergeBlast/Candy"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Fill ("Fill", Range(0,1)) = 1
        _Flash ("Flash", Range(0,1)) = 0
        _Gold ("Gold", Float) = 0
        _Rainbow ("Rainbow", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Fill;
                half _Flash;
                half _Gold;
                half _Rainbow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionOS = input.positionOS.xyz;
                return o;
            }

            half3 Hue(float h)
            {
                return saturate(abs(frac(h + half3(0, 0.667, 0.333)) * 6 - 3) - 1);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 light = normalize(float3(-0.45, 0.7, -0.55));
                float3 view = float3(0, 0, -1);

                half3 base = _Color.rgb;
                if (_Rainbow > 0.5)
                    base = lerp(Hue((input.positionOS.x + input.positionOS.y) * 0.7 + _Time.y * 0.6), 1, 0.15);

                // уровень "жидкости" внутри шара показывает остаток здоровья
                if (_Fill < 0.999)
                {
                    float level = input.positionOS.y + 0.5 + sin(input.positionOS.x * 14 + _Time.y * 5) * 0.015;
                    if (level > _Fill) base *= 0.45;
                    else if (level > _Fill - 0.035) base = lerp(base, 1, 0.4);
                }

                float diffuse = dot(n, light) * 0.5 + 0.5;
                half3 color = base * lerp(0.55, 1.15, diffuse * diffuse);
                float facing = saturate(dot(n, view));

                if (_Gold > 0.5 && facing < 0.62)
                {
                    float shine = pow(saturate(sin(atan2(n.y, n.x) * 2 - _Time.y * 3)), 6);
                    color = lerp(half3(0.95, 0.6, 0.05), half3(1, 0.88, 0.3), diffuse) + shine * 0.5;
                }
                else
                {
                    color += pow(1 - facing, 3) * 0.35;
                }

                float spec = pow(saturate(dot(n, normalize(light + view))), 40);
                color += smoothstep(0.3, 0.6, spec) * 0.7;
                color = lerp(color, 1, _Flash);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
