#ifndef PAINTWAVE_INCLUDED
#define PAINTWAVE_INCLUDED

// Animated liquid surface for BottledPaintShader.
//
// Height comes back in world Y units so it adds straight onto the fill level,
// while the pattern is sampled in object XZ so each bottle ripples on its own
// and the waves travel with it.

void PaintWave_float(
    float3 PositionOS,
    float2 Direction,
    float Speed,
    float Frequency,
    float Amplitude,
    float Fill,
    float Time,
    out float Height,
    out float3 NormalWS,
    out float2 SurfaceUV)
{
    // Primary direction, guarded so a zeroed _WaveDirection still animates.
    float lenSq = dot(Direction, Direction);
    float2 d1 = Direction * rsqrt(max(lenSq, 1e-8));
    if (lenSq < 1e-8)
        d1 = float2(1.0, 0.0);

    // Cross wave: perpendicular to d1, then swung back towards it so the two
    // sines interfere instead of forming an obvious square grid.
    float2 d2 = normalize(float2(-d1.y, d1.x) * 0.75 + d1 * 0.35);

    float2 p = PositionOS.xz;
    float t = Time * Speed;

    float k1 = Frequency;
    float k2 = Frequency * 1.7;

    float ph1 = dot(p, d1) * k1 + t;
    float ph2 = dot(p, d2) * k2 - t * 0.85;

    // Fade the ripple out as the surface nears the brim or the floor, so crests
    // cannot clip through the ends of the mesh.
    float fade = smoothstep(0.0, 0.05, Fill) * smoothstep(0.0, 0.05, 1.0 - Fill);
    float a1 = Amplitude * fade;
    float a2 = a1 * 0.45;

    Height = a1 * sin(ph1) + a2 * sin(ph2);

    // Analytic gradient of Height with respect to object XZ.
    float2 g = a1 * cos(ph1) * k1 * d1 + a2 * cos(ph2) * k2 * d2;

    // A gradient is a covector, so it rotates with the inverse transpose. Only
    // the pattern rides the object; the surface itself stays level in world
    // space, matching the world-space fill plane. Amplitude 0 gives (0,1,0).
    float3 gWS = TransformObjectToWorldNormal(float3(g.x, 0.0, g.y), false);
    NormalWS = SafeNormalize(float3(-gWS.x, 1.0, -gWS.z));

    // Planar UV for the liquid surface. Crests sit where dot(p,d1)*k1 + t is
    // constant, so they travel at -d1/k1; offsetting by +d1*t/k1 makes a texture
    // sampled here ride exactly with them instead of drifting at some invented
    // speed. Tiling is applied downstream in the graph.
    SurfaceUV = p + d1 * (t / max(k1, 1e-4));
}

void PaintWave_half(
    half3 PositionOS,
    half2 Direction,
    half Speed,
    half Frequency,
    half Amplitude,
    half Fill,
    half Time,
    out half Height,
    out half3 NormalWS,
    out half2 SurfaceUV)
{
    float height;
    float3 normalWS;
    float2 surfaceUV;
    PaintWave_float(PositionOS, Direction, Speed, Frequency, Amplitude, Fill, Time,
                    height, normalWS, surfaceUV);
    Height = height;
    NormalWS = normalWS;
    SurfaceUV = surfaceUV;
}

#endif // PAINTWAVE_INCLUDED
