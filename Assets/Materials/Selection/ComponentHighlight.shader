// Selection effect that is drawn ON TOP of a component's normal material (added as a second material
// only while the component is selected, so it costs nothing for all other objects).
//   Pass "Outline": the back faces, pushed outwards in screen space by _OutlineWidth pixels -> a glowing outline.
//                   Uses the smoothed normals baked into UV3 by SmoothNormalsPostprocessor; the voxel models
//                   have flat normals, and with those the outline would break apart at every corner.
//   Pass "Glow":    additive fresnel rim + a light band that sweeps over the model + a soft fill.
// Calm by default: on selection the band sweeps over the model ONCE and the pulse fades out, then the effect
// stays still (constant motion next to a puzzle you think about for minutes is tiring).
// _Intensity (0..1), _SelectTime and _ScanRange are set from code (SelectionHighlight).
Shader "RISCadventure/Component Highlight"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0.25, 0.85, 1, 1)
        _OutlineWidth ("Outline Width (pixels)", Range(0, 12)) = 3

        [HDR] _RimColor ("Rim Color", Color) = (0.25, 0.85, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _FillStrength ("Fill Strength", Range(0, 1)) = 0.12

        [HDR] _ScanColor ("Scan Band Color", Color) = (0.6, 0.95, 1, 1)
        _ScanWidth ("Scan Band Width (world units)", Float) = 0.25
        _ScanSpacing ("Scan Band Repeat (world units)", Float) = 4
        _ScanSpeed ("Scan Band Speed, loop mode (world units/s)", Float) = 2
        [ToggleUI] _ScanLoop ("Scan Band Loops Forever", Float) = 0
        _ScanOnceDuration ("Scan Band Duration, once mode (s)", Float) = 0.8

        _PulseSpeed ("Pulse Speed", Float) = 4
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        _PulseDecay ("Pulse Fade Out (1/s, 0 = pulse forever)", Float) = 1.5

        _Intensity ("Intensity (set by code)", Range(0, 1)) = 1
        _SelectTime ("Select Time (set by code)", Float) = -1000
        _ScanRange ("Scan Range (set by code)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            float _OutlineWidth;
            half4 _RimColor;
            float _RimPower;
            float _FillStrength;
            half4 _ScanColor;
            float _ScanWidth;
            float _ScanSpacing;
            float _ScanSpeed;
            float _ScanLoop;
            float _ScanOnceDuration;
            float _PulseSpeed;
            float _PulseAmount;
            float _PulseDecay;
            float _Intensity;
            float _SelectTime;
            float4 _ScanRange;
        CBUFFER_END

        // Seconds since the component was selected (_Time.y is Time.timeSinceLevelLoad).
        float TimeSinceSelect()
        {
            return max(_Time.y - _SelectTime, 0.0);
        }

        float Pulse()
        {
            float amount = _PulseAmount * (_PulseDecay > 0.0 ? exp(-TimeSinceSelect() * _PulseDecay) : 1.0);
            return 1.0 - amount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
        }
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float3 smoothNormal : TEXCOORD3; // baked by SmoothNormalsPostprocessor, (0,0,0) if missing
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalOS = dot(input.smoothNormal, input.smoothNormal) > 0.0001 ? input.smoothNormal : input.normalOS;

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(normalOS));

                float2 direction = normalCS.xy;
                float len = length(direction);
                direction = len > 0.00001 ? direction / len : float2(0, 0);

                // Constant width in pixels, independent of the zoom: multiply by w before the perspective divide.
                positionCS.xy += direction * (_OutlineWidth * 2.0 / _ScreenParams.xy) * positionCS.w;
                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb * Pulse(), _OutlineColor.a * _Intensity);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Glow"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

                // Fresnel: bright where the surface turns away from the camera (the side walls of the model).
                float rim = pow(1.0 - saturate(dot(normal, viewDirection)), _RimPower);

                // A band of light moving diagonally over the model.
                float along = input.positionWS.x + input.positionWS.z;
                float distanceToBand;
                if (_ScanLoop > 0.5)
                {
                    // Forever, repeating every _ScanSpacing units.
                    float moving = along - _Time.y * _ScanSpeed;
                    distanceToBand = abs(frac(moving / _ScanSpacing) - 0.5) * _ScanSpacing;
                }
                else
                {
                    // Once: from one end of the model (_ScanRange.x) to the other (_ScanRange.y), then gone.
                    float progress = TimeSinceSelect() / max(_ScanOnceDuration, 0.0001);
                    float center = lerp(_ScanRange.x - _ScanWidth, _ScanRange.y + _ScanWidth, progress);
                    distanceToBand = abs(along - center);
                }
                float band = saturate(1.0 - distanceToBand / max(_ScanWidth, 0.0001));
                band *= band;

                float3 color = (_RimColor.rgb * (rim + _FillStrength) * Pulse() + _ScanColor.rgb * band) * _Intensity;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
