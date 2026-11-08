// Tufos de capim (v0.9.7): duas placas cruzadas com textura pixel art, balançando devagar com o vento.
// Só a parte de cima mexe (UV.y = 0 no topo). Cor por instância (MultiMesh com cores) varia o verde.
shader_type spatial;
render_mode cull_disabled, diffuse_lambert, specular_disabled;

uniform sampler2D tuft_tex : source_color, filter_nearest_mipmap;
uniform float sway = 0.07;
uniform float wind_speed = 0.8;

varying vec3 tint;

void vertex() {
	vec3 w = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	float top = 1.0 - UV.y;
	float phase = TIME * wind_speed + w.x * 0.31 + w.z * 0.23;
	VERTEX.x += sin(phase) * sway * top;
	VERTEX.z += sin(phase * 0.7 + 1.3) * sway * 0.5 * top;
	// Luz como se fosse o chão (sem placas escuras quando viradas de costas pro sol).
	NORMAL = vec3(0.0, 1.0, 0.0);
	tint = COLOR.rgb;
}

void fragment() {
	vec4 t = texture(tuft_tex, UV);
	if (t.a < 0.5) {
		discard;
	}
	ALBEDO = t.rgb * tint;
	ROUGHNESS = 1.0;
}
