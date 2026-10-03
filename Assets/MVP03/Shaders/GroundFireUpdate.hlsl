// Keep the buoyant direction while Curl Noise changes the three-dimensional trajectory.
void UpdateGroundFire(inout VFXAttributes attributes, float DeltaTime, float RiseSpeed,
    float FlameWidth, float FlameHeight, float Brightness, float AnimationFPS, float3 ParticlePivot)
{
    float life = saturate(attributes.age / max(attributes.lifetime,.001));
    // Apply to existing particles too, so live pivot adjustments do not wait for respawn.
    attributes.pivotX = ParticlePivot.x;
    attributes.pivotY = ParticlePivot.y;
    attributes.pivotZ = ParticlePivot.z;
    attributes.velocity.y = max(attributes.velocity.y, RiseSpeed * .6);
    attributes.velocity.xz *= exp(-DeltaTime * 1.4);
    attributes.size = FlameWidth * (.8 + .25 * sin(life * 3.14159)) * (1 - life * .56);
    attributes.scaleY = lerp(1.8,2.6,life) * FlameHeight / 1.05;
    attributes.color = lerp(float3(1,.76,.045), float3(1,.22,.003), smoothstep(.25,1,life)) * Brightness;
    attributes.alpha = smoothstep(0,.12,life) * pow(saturate(1-life),1.1) * .28;
    attributes.texIndex = fmod(attributes.texIndex + DeltaTime * AnimationFPS,16);
}
