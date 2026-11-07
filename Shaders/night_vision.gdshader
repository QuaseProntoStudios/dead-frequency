// Filtro de visão noturna: amplifica a luz da tela, deixa tudo verde, com ruído e vinheta.
shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float gain = 5.0;
uniform vec3 tint : source_color = vec3(0.3, 1.0, 0.4);

float rand(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec3 c = texture(screen_tex, SCREEN_UV).rgb;
	float l = dot(c, vec3(0.3, 0.59, 0.11));
	l = 1.0 - exp(-l * gain);
	l += (rand(SCREEN_UV * 431.0 + fract(TIME * 7.0)) - 0.5) * 0.12;
	float scan = 0.9 + 0.1 * sin(SCREEN_UV.y * 900.0 + TIME * 20.0);
	float vignette = smoothstep(0.78, 0.3, length(SCREEN_UV - 0.5));
	COLOR = vec4(tint * clamp(l, 0.0, 1.2) * scan * vignette, 1.0);
}
