// Calçada de placas de concreto com junta a cada 1,5 m e meio-fio mais claro.
shader_type spatial;
render_mode diffuse_lambert;

uniform vec3 tile_color : source_color = vec3(0.36, 0.35, 0.33);
uniform float tile_size = 1.5;

varying vec3 world_pos;
varying vec3 world_normal;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec3 col = tile_color;
	if (world_normal.y > 0.5) {
		vec2 cell = world_pos.xz / tile_size;
		vec2 f = fract(cell);
		col += hash(floor(cell)) * 0.06 - 0.03;
		if (f.x < 0.04 || f.y < 0.04) {
			col *= 0.7;
		}
	} else {
		col = vec3(0.5, 0.49, 0.46); // meio-fio
	}
	ALBEDO = col;
	ROUGHNESS = 0.95;
}
