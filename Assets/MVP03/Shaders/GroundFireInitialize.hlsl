// Custom HLSL Initialize block. Coverage stays on the GPU; dry births are rejected.
void InitializeGroundFire(inout VFXAttributes attributes, VFXSampler2D Coverage,
    float4 Atlas, float4 Region, float Elevation, float FlameHeight, float RiseSpeed,
    float Threshold, float EdgeSoftness, float3 ParticlePivot)
{
    float3 random = VFXRAND3;
    float2 p = Region.xy + random.xy * Region.zw;
    float2 uv = (p - Atlas.xy) / Atlas.zw;
    float mask = smoothstep(Threshold - EdgeSoftness, Threshold + EdgeSoftness, SampleTexture(Coverage, uv, 0).r);
    attributes.alive = mask > VFXRAND;
    attributes.position = float3(p.x, Elevation + 0.015, p.y);
    attributes.velocity = float3((random.x - .5) * .22, RiseSpeed * lerp(.7,1.05,random.z), (random.y - .5) * .22);
    attributes.lifetime = lerp(.55,.95,random.z) * FlameHeight / max(RiseSpeed,.2);
    attributes.age = 0;
    attributes.mass = 1;
    attributes.size = .5;
    attributes.scaleX = 1;
    attributes.scaleY = 1.8;
    attributes.scaleZ = 1;
    attributes.pivotX = ParticlePivot.x;
    attributes.pivotY = ParticlePivot.y;
    attributes.pivotZ = ParticlePivot.z;
    attributes.texIndex = random.z * 15;
    attributes.color = float3(1,1,1);
    attributes.alpha = 0;
}
