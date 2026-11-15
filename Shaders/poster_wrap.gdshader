// Papel envelhecido aplicado a uma folha parcialmente curvada ao redor do poste.
// UV.x percorre a largura do papel; UV.y percorre sua altura.
shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec4 paper_color : source_color = vec4(0.92, 0.86, 0.68, 1.0);

float hash21(vec2 p) {
    p = fract(p * vec2(127.1, 311.7));
    p += dot(p, p + 19.19);
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
    float fiber = sin(UV.y * 190.0 + noise2(vec2(UV.x * 22.0, UV.y * 8.0)) * 4.0);
    float stains = noise2(vec2(UV.x * 13.0, UV.y * 17.0));
    float horizontalEdge = smoothstep(0.0, 0.055, min(UV.x, 1.0 - UV.x));
    float verticalEdge = smoothstep(0.0, 0.045, min(UV.y, 1.0 - UV.y));
    float intact = horizontalEdge * verticalEdge;
    vec3 paper = paper_color.rgb * (0.9 + fiber * 0.035 + stains * 0.08);
    paper *= mix(0.68, 1.0, intact);
    ALBEDO = paper;
    ROUGHNESS = 1.0;
    SPECULAR = 0.0;
}
