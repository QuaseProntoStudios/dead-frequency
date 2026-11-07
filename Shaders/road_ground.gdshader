// Chão inteiro da cidade: asfalto nas faixas das ruas (com faixas pintadas e faixa de pedestre)
// e concreto no resto. As ruas ficam em k*period + period/2, nos dois eixos.
shader_type spatial;
render_mode diffuse_lambert;

uniform float period = 48.0;
uniform float road_half_width = 6.0;
uniform vec3 asphalt : source_color = vec3(0.09, 0.09, 0.1);
uniform vec3 concrete : source_color = vec3(0.3, 0.29, 0.27);
uniform vec3 line_yellow : source_color = vec3(0.75, 0.6, 0.15);
uniform vec3 line_white : source_color = vec3(0.7, 0.7, 0.68);

varying vec3 world_pos;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 p = world_pos.xz;
	// Coordenada relativa ao centro da rua mais próxima em cada eixo.
	vec2 q = mod(p, period) - period * 0.5;
	vec2 d = abs(q);
	bool road_x = d.x < road_half_width; // rua que corre ao longo de Z
	bool road_z = d.y < road_half_width; // avenida que corre ao longo de X
	float noise = hash(floor(p * 3.0)) * 0.05;
	vec3 col = concrete + noise - 0.025;
	float rough = 0.95;

	if (road_x || road_z) {
		col = asphalt + noise;
		rough = 0.85;
		bool intersection = road_x && road_z;
		if (!intersection) {
			float along = road_x ? p.y : p.x;
			float across = road_x ? d.x : d.y;
			float across_signed = road_x ? q.x : q.y;
			float cross_dist = road_x ? d.y : d.x; // distância até o cruzamento
			bool near_crosswalk = cross_dist < road_half_width + 3.6;
			// Faixa central amarela tracejada (some perto da faixa de pedestre).
			if (!near_crosswalk && across < 0.1 && fract(along / 6.0) < 0.5) {
				col = line_yellow;
			}
			// Linha de retenção (PARE) antes da faixa de pedestre, só na pista da direita.
			if (cross_dist > road_half_width + 3.25 && cross_dist < road_half_width + 3.6 && across < road_half_width - 0.7) {
				float approach = road_x ? q.y : q.x; // de que lado do cruzamento
				float lane = across_signed;
				if ((approach > 0.0 && lane > 0.0) || (approach < 0.0 && lane < 0.0)) {
					col = line_white;
				}
			}
			// Bordas brancas.
			if (across > road_half_width - 0.55 && across < road_half_width - 0.4) {
				col = line_white;
			}
			// Faixa de pedestre perto do cruzamento.
			if (cross_dist > road_half_width + 0.5 && cross_dist < road_half_width + 3.0 && fract(across_signed / 1.0) < 0.5 && across < road_half_width - 0.7) {
				col = line_white * 0.9;
			}
			// Mancha de óleo ocasional.
			if (hash(floor(p / 4.0)) > 0.93) {
				col *= 0.7;
			}
		}
	}

	ALBEDO = col;
	ROUGHNESS = rough;
}
