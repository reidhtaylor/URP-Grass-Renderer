#ifndef GRASS_BLADES_COMPUTE_HELP_INCLUDED
#define GRASS_BLADES_COMPUTE_HELP_INCLUDED

void GetNormalAndTSToWSMatrix(float3 normalWS, out float3x3 tangentToWorld) {
    float3 tangentWS = float3(1, 0, 0);
    float3 bitangentWS = normalize(cross(tangentWS, normalWS));
    tangentToWorld = transpose(float3x3(tangentWS, bitangentWS, normalWS));
}

float3 GetTriangleCenter(float3 a, float3 b, float3 c) {
    return (a + b + c) / 3.0;
}
float2 GetTriangleCenter(float2 a, float2 b, float2 c) {
    return (a + b + c) / 3.0;
}

float rand(float4 value) {
    float4 smallValue = sin(value);
    float random = dot(smallValue, float4(12.9898, 78.233, 37.719, 09.151));
    random = frac(sin(random) * 143758.5453);
    return random;
}

float rand(float3 pos, float offset) {
    return rand(float4(pos, offset));
}

float randNegative1to1(float3 pos, float offset) {
    return rand(pos, offset) * 2 - 1;
}

float3x3 AngleAxis3x3(float angle, float3 axis) {
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
}

#endif