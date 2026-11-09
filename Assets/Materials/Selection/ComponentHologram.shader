// "Hologram" look for a selected component. Unlike Component Highlight this REPLACES the normal material
// while the component is selected: the model becomes semi-transparent with glowing edges, scan lines that
// run over the screen and a light flicker. No shadow caster pass, so the selected model stops casting a shadow.
// _Intensity (0..1) is animated from code for the fade in/out.
Shader "RISCadventure/Component Hologram"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 0.8, 1, 1)
        _Alpha ("Body Alpha", Range(0, 1)) = 0.25
        _RimPower ("Edge Power", Range(0.5, 8)) = 2
        _RimStrength ("Edge Strength", Range(0, 3)) = 1.2

        _LineSpacing ("Scan Line Spacing (pixels)", Float) = 6
        _LineSpeed ("Scan Line Speed", Float) = 20
        _LineStrength ("Scan Line Strength", Range(0, 1)) = 0.35

        _FlickerSpeed ("Flicker Speed", Float) = 18
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.08

        _Intensity ("Intensity (set by code)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On   // only the front-most faces of the model, otherwise the voxels shine through each other
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Alpha;
                float _RimPower;
                float _RimStrength;
                float _LineSpacing;
                float _LineSpeed;
                float _LineStrength;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim = pow(1.0 - saturate(dot(normal, viewDirection)), _RimPower) * _RimStrength;

                // SV_POSITION.y in the fragment shader is the pixel row -> lines with a fixed pixel spacing.
                float lines = 0.5 + 0.5 * sin((input.positionCS.y + _Time.y * _LineSpeed) * 6.2831853 / max(_LineSpacing, 1.0));
                float flicker = 1.0 - _FlickerAmount * step(0.5, frac(sin(floor(_Time.y * _FlickerSpeed)) * 43758.5453));

                float alpha = saturate(_Alpha + rim + lines * _LineStrength * 0.5) * flicker * _Intensity;
                float3 color = _Color.rgb * (1.0 + rim + lines * _LineStrength);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
