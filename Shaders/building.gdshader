// Fachada procedural: janelas calculadas pela posição no mundo, então qualquer
// tamanho de prédio funciona com um único material. Cor e sorteio por instância.
shader_type spatial;
render_mode diffuse_lambert, specular_schlick_ggx;

instance uniform vec4 wall_color : source_color = vec4(0.45, 0.42, 0.38, 1.0);
instance uniform float light_seed = 0.0;
instance uniform float lit_ratio = 0.3;
instance uniform float building_height = 20.0;
/** Acabamento da fachada (v0.9.7): 0 reboco, 1 tijolo, 2 pastilha, 3 chapisco. */
instance uniform float wall_kind = 0.0;

/** Atlas 2x2 de acabamentos (Assets/Textures/City/wall_atlas.png), cada tile = 2 m. */
uniform sampler2D wall_atlas : source_color, filter_nearest_mipmap, repeat_enable;
uniform float tile_meters = 2.0;

/** 1 = noite (muitas janelas acesas), 0 = dia (poucas, fracas). */
uniform float night : hint_range(0.0, 1.0) = 1.0;
uniform float floor_height = 3.2;
uniform float window_spacing = 2.6;
uniform vec3 warm_light : source_color = vec3(1.0, 0.78, 0.45);
uniform vec3 cold_light : source_color = vec3(0.55, 0.85, 1.0);

varying vec3 world_pos;
varying vec3 world_normal;
varying vec3 local_pos;
varying float facade_local_u;
varying float facade_axis_scale;
varying float facade_edge_distance;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
	local_pos = VERTEX;
	bool x_facing = abs(NORMAL.x) > 0.5;
	facade_local_u = x_facing ? VERTEX.z : VERTEX.x;
	facade_axis_scale = x_facing ? length(MODEL_MATRIX[2].xyz) : length(MODEL_MATRIX[0].xyz);
	facade_edge_distance = max(0.0, (0.5 - abs(facade_local_u)) * facade_axis_scale);
}

void fragment() {
	vec3 col = wall_color.rgb;
	vec3 emit = vec3(0.0);
	float rough = 0.92;
	float kind = floor(wall_kind + 0.5);

	// Grão estilo PS1 (ruído em blocos de 25 cm).
	float grain = hash(floor(world_pos.xz * 4.0) + floor(world_pos.y * 4.0)) * 0.08 - 0.04;
	col += grain;

	if (abs(world_normal.y) < 0.5) {
		float u = abs(world_normal.x) > 0.5 ? world_pos.z : world_pos.x;
		// Textura do acabamento: tile do atlas escolhido por wall_kind, multiplicado pela cor.
		vec2 full = vec2(u, -world_pos.y) / tile_meters;
		vec2 tile = vec2(mod(kind, 2.0), floor(kind / 2.0));
		// textureGrad com a derivada da coordenada sem fract: evita a linha de mipmap na emenda do tile.
		vec3 tex = textureGrad(wall_atlas, (tile + fract(full)) * 0.5, dFdx(full) * 0.5, dFdy(full) * 0.5).rgb;
		col = kind == 1.0 ? mix(col * tex * 1.8, tex, 0.75) : col * tex * 1.8;
		col += grain * 0.5;
		// Janelas repetidas em espaçamento fixo no mundo: mantém várias divisões
		// pequenas em cada fachada, independentemente da escala do BoxMesh.
		float relative_y = (local_pos.y + 0.5) * building_height;
		vec2 cell = vec2(u / window_spacing, relative_y / floor_height);
		vec2 f = fract(cell);
		vec2 id = floor(cell);
		// Faixa entre andares.
		if (f.y < 0.08) {
			col *= 0.75;
		}
		float window_top = (id.y + 0.82) * floor_height;
		bool complete_window = window_top <= building_height - 0.25;

		// Só desenha a janela se a abertura inteira couber antes da quina.
		// O centro da célula é reconstruído a partir da posição interpolada.
		float window_center_u = facade_local_u + (0.5 - f.x) * window_spacing / max(facade_axis_scale, 0.001);
		float window_center_edge_distance = (0.5 - abs(window_center_u)) * facade_axis_scale;
		float window_half_width = window_spacing * 0.30;
		bool fits_before_corner = window_center_edge_distance >= window_half_width + 0.08;

		bool window_area = f.x > 0.2 && f.x < 0.8 && f.y > 0.32 && f.y < 0.82
			&& id.y >= 1.0 && complete_window && fits_before_corner;

		// Peitoril claro embaixo da janela; também não pode atravessar a quina.
		if (id.y >= 1.0 && f.x > 0.17 && f.x < 0.83 && f.y > 0.28 && f.y < 0.32
			&& complete_window && fits_before_corner) {
			col = vec3(0.62, 0.6, 0.56);
		}
		if (window_area) {
			// Sorteio individual por janela.
			// A coluna mais próxima da quina usa uma chave compartilhada entre as duas fachadas.
			float corner_key = (sign(local_pos.x) + 1.0) * 2.0 + (sign(local_pos.z) + 1.0);
			bool corner_column = window_center_edge_distance < window_half_width + window_spacing * 0.9;
			float h = corner_column
				? hash(vec2(corner_key + light_seed, id.y + light_seed * 1.7))
				: hash(vec2(id.x + light_seed * 13.0, id.y + light_seed * 1.7));
			col = vec3(0.05, 0.06, 0.07);
			rough = 0.25;
			float ratio = lit_ratio * mix(0.08, 1.0, night);
			if (h < ratio) {
				vec3 lamp = h < ratio * 0.18 ? cold_light : warm_light;
				// Algumas janelas piscam devagar (TV ligada).
				float flicker = h < ratio * 0.06 ? 0.6 + 0.4 * sin(TIME * 7.0 + h * 40.0) : 1.0;
				col = lamp * 0.6;
				emit = lamp * 1.4 * flicker * mix(0.2, 1.0, night);
			}
		}
		// Térreo: faixa escura de lojas fechadas.
		if (id.y < 1.0 && f.y > 0.15 && f.y < 0.75) {
			col = vec3(0.07, 0.07, 0.08);
		}
	} else {
		col *= 0.55; // telhado
	}

	ALBEDO = col;
	EMISSION = emit;
	ROUGHNESS = rough;
}
