// Electric spark for the signals that run along the wires (inspired by the zipline sparks in A Hat in Time):
// a white-hot core with a soft glow and yellow jagged arcs around it that jump to new random shapes
// _FrameRate times per second, like hand-drawn frames. Fully procedural – no texture needed.
// Put it on a Unity Quad. The quad always faces the camera (billboard in the vertex shader), so no script is needed.
// Blending is premultiplied: the arcs are solid (readable on bright grass), the core and glow add light.
Shader "RISCadventure/Electric Spark"
{
    Properties
    {
        [MainColor] [HDR] _BaseColor ("Tint (used by the selection colour)", Color) = (1, 1, 1, 1)
        [HDR] _ArcColor ("Arc Color", Color) = (1, 0.85, 0.25, 1)
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 0.9, 1)
        _HaloStrength ("Glow Strength", Range(0, 1)) = 0.35
        _CoreSharpness ("Core Sharpness", Range(2, 60)) = 18

        _ArcRadius ("Arc Radius (min, max) 0..1", Vector) = (0.45, 0.75, 0, 0)
        _ArcThickness ("Arc Thickness", Range(0.005, 0.2)) = 0.05
        _Jaggedness ("Arc Jaggedness (zigzags)", Range(1, 30)) = 9
        _JagAmount ("Arc Zigzag Height", Range(0, 0.4)) = 0.12
        _FrameRate ("Frames Per Second", Range(1, 60)) = 14

        _Size ("Size", Float) = 1
        _Seed ("Seed (set by code)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Spark"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha // premultiplied alpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ArcColor;
                half4 _CoreColor;
                float _HaloStrength;
                float _CoreSharpness;
                float4 _ArcRadius;
                float _ArcThickness;
                float _Jaggedness;
                float _JagAmount;
                float _FrameRate;
                float _Size;
                float _Seed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Billboard: build the quad from the camera's right/up axes around the object's centre.
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float scale = length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)) * _Size;
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = centerWS + (input.positionOS.x * right + input.positionOS.y * up) * scale;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Noise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                float u = f * f * (3.0 - 2.0 * f);
                return lerp(Hash(i), Hash(i + 1.0), u);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;      // -1..1, centre of the quad = 0
                float r = length(p);
                float angle = atan2(p.y, p.x);

                // A new random shape every 1/_FrameRate seconds, different for every signal (_Seed).
                float frame = floor(_Time.y * _FrameRate) + _Seed * 17.0;

                float core = exp(-r * r * _CoreSharpness);
                float glow = exp(-r * r * 3.0) * _HaloStrength;

                // Three jagged arc pieces around the core.
                float arcs = 0.0;
                float pixel = fwidth(r);
                [unroll] for (int i = 0; i < 3; i++)
                {
                    float fi = (float)i;
                    float centerAngle = Hash(frame * 1.37 + fi * 11.1) * 6.2831853 - 3.1415927;
                    float halfSpan = lerp(0.35, 0.8, Hash(frame * 2.11 + fi * 5.7));
                    float deltaAngle = abs(atan2(sin(angle - centerAngle), cos(angle - centerAngle)));
                    float inSector = saturate((halfSpan - deltaAngle) / 0.15);

                    float radius = lerp(_ArcRadius.x, _ArcRadius.y, Hash(frame * 3.73 + fi * 2.3))
                                 + (Noise(angle * _Jaggedness + frame * 7.0 + fi * 13.0) - 0.5) * _JagAmount;
                    float onLine = 1.0 - smoothstep(_ArcThickness - pixel, _ArcThickness + pixel, abs(r - radius));
                    arcs = max(arcs, onLine * inSector);
                }

                float edgeFade = 1.0 - smoothstep(0.85, 1.0, r);
                float3 tint = _BaseColor.rgb;
                float alpha = arcs * _ArcColor.a * edgeFade;
                float3 color = (_ArcColor.rgb * alpha + _CoreColor.rgb * (core + glow) * edgeFade) * tint;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
