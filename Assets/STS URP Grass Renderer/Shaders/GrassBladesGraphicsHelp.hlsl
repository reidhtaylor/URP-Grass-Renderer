#ifndef GRASS_BLADES_GRAPHICS_HELP_INCLUDED
#define GRASS_BLADES_GRAPHICS_HELP_INCLUDED

float3 GetViewDirectionFromPosition(float3 positionWS) {
    return normalize(GetCameraPositionWS() - positionWS);
}

#ifdef SHADOW_CASTER_PASS
float3 _LightDirection;
#endif

float4 CalculatePositionCSWithShadowCasterLogic(float3 positionWS, float3 normalWS) {
    float4 positionCS;

#ifdef SHADOW_CASTER_PASS
    positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
#else
    positionCS = TransformWorldToHClip(positionWS);
#endif

    return positionCS;
}

float4 CalculateShadowCoord(float3 positionWS, float4 positionCS) {
#if SHADOWS_SCREEN
    return ComputeScreenPos(positionCS);
#else
    return TransformWorldToShadowCoord(positionWS);
#endif
}

#endif