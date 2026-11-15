// Textura procedural para postes antigos de madeira, em escala discreta para a estética PS1.
shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float noise2(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void fragment() {
    // UV.x acompanha a circunferência e cria veios compridos no sentido do poste.
    float broad = noise2(vec2(UV.x * 8.0, UV.y * 5.0));
    float fine = sin(UV.x * 150.0 + noise2(vec2(UV.x * 7.0, UV.y * 18.0)) * 5.0);
    float cracks = smoothstep(0.68, 0.9, noise2(vec2(UV.x * 34.0, UV.y * 6.0)));
    vec3 darkWood = vec3(0.19, 0.13, 0.085);
    vec3 warmWood = vec3(0.39, 0.28, 0.17);
    vec3 weathered = vec3(0.34, 0.34, 0.29);
    vec3 wood = mix(darkWood, warmWood, broad);
    wood = mix(wood, weathered, smoothstep(0.38, 0.82, noise2(vec2(UV.x * 6.0, UV.y * 3.0))) * 0.48);
    wood *= 0.91 + fine * 0.055;
    wood *= 1.0 - cracks * 0.22;
    ALBEDO = wood;
    ROUGHNESS = 0.96;
    SPECULAR = 0.06;
}
