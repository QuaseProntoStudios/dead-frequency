// Chão da fazenda: capim com manchas secas e estradas de terra desenhadas pelo próprio shader
// (sem malhas sobrepostas, então nada de "piscar" nos cruzamentos). As estradas chegam do
// CityGenerator.Farm.cs como segmentos: road_data[i*5..i*5+4] = ax, az, bx, bz, largura.
shader_type spatial;
render_mode diffuse_lambert, specular_disabled;

const int MAX_ROADS = 64;
uniform float road_data[320];
uniform int road_count = 0;
/// Lago: x, z, raio (raio <= 0 = sem lago).
uniform vec3 lake = vec3(0.0, 0.0, -1.0);
/// Metade do lado do pátio de cascalho do depósito (centrado na origem).
uniform float yard_half = 18.0;

varying vec3 world_pos;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
			mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}

float seg_dist(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / max(dot(ba, ba), 0.0001), 0.0, 1.0);
	return length(pa - ba * h);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 p = world_pos.xz;
	float n1 = noise(p * 0.035);
	float n2 = noise(p * 0.31);
	float n3 = noise(p * 2.7);

	// Capim: verde que varia em manchas grandes, com áreas secas amareladas.
	vec3 grass = mix(vec3(0.13, 0.22, 0.07), vec3(0.24, 0.31, 0.1), n1);
	grass = mix(grass, vec3(0.34, 0.3, 0.14), smoothstep(0.62, 0.9, n1 * 0.6 + n2 * 0.5));
	grass *= 0.82 + 0.36 * n3;

	// Estradas de terra com marcas de pneu.
	float road = 0.0;
	float nearest = 1000.0;
	float nearest_hw = 4.0;
	for (int i = 0; i < MAX_ROADS; i++) {
		if (i >= road_count) {
			break;
		}
		int k = i * 5;
		float d = seg_dist(p, vec2(road_data[k], road_data[k + 1]), vec2(road_data[k + 2], road_data[k + 3]));
		float hw = road_data[k + 4] * 0.5;
		float edge = n2 * 0.9;
		road = max(road, 1.0 - smoothstep(hw - 0.9 + edge, hw + 0.5 + edge, d));
		if (d - hw < nearest - nearest_hw) {
			nearest = d;
			nearest_hw = hw;
		}
	}
	vec3 dirt = mix(vec3(0.33, 0.23, 0.14), vec3(0.44, 0.32, 0.19), n2) * (0.82 + 0.36 * n3);
	if (nearest_hw > 3.0) {
		float track = 1.0 - smoothstep(0.18, 0.45, abs(nearest - nearest_hw * 0.38));
		dirt *= 1.0 - track * 0.22;
		// Capim no meio da estrada.
		dirt = mix(dirt, grass * 0.9, (1.0 - smoothstep(0.2, 0.6, nearest)) * step(0.45, n3) * 0.6);
	}
	vec3 col = mix(grass, dirt, road);

	// Pátio de cascalho do depósito.
	float yard = 1.0 - smoothstep(yard_half - 0.5, yard_half + 2.0, max(abs(p.x), abs(p.y)));
	vec3 gravel = vec3(0.4, 0.39, 0.36) * (0.72 + 0.56 * n3);
	col = mix(col, gravel, yard);

	// Margem de barro em volta do lago.
	if (lake.z > 0.0) {
		float dl = length(p - lake.xy);
		col = mix(col, vec3(0.19, 0.15, 0.09) * (0.8 + 0.4 * n3), 1.0 - smoothstep(lake.z, lake.z + 4.5 + n2 * 2.0, dl));
	}

	ALBEDO = col;
	ROUGHNESS = 0.96;
}
