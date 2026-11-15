shader_type spatial;
// Grade 3D xadrez (shader "3D Grid" do Matheus, pasta Shaders Download/test).
// Textura provisória: usada onde ainda não há textura própria (ex.: muros do limite da cidade).

const float EPSILON = 0.0001;

uniform vec3 color_a : source_color = vec3(0.88, 0.91, 0.91);
uniform vec3 color_b : source_color = vec3(0.78, 0.80, 0.86);
uniform float cells = 0.5; // células por metro (0.5 = quadrados de 2 m)
uniform bool world_coordinates = true;

varying vec3 local_position;

float apply_floor_bias(float value) {
	if (floor(value) != floor(value + EPSILON)) {
		value += EPSILON;
	}
	return value;
}

vec3 snap_to_grid(vec3 position) {
	vec3 grid_position = (position - vec3(1.0 / cells)) * cells;
	grid_position = vec3(apply_floor_bias(grid_position.x), apply_floor_bias(grid_position.y), apply_floor_bias(grid_position.z));
	return floor(grid_position);
}

vec3 sample_3d_grid(vec3 position) {
	vec3 cell_parity = mod(snap_to_grid(position), 2.0);
	bool diagonal = (abs(cell_parity.x - cell_parity.z) <= EPSILON) != (abs(cell_parity.y) <= EPSILON);
	return mix(color_a, color_b, float(diagonal));
}

void vertex() {
	local_position = VERTEX;
}

void fragment() {
	vec3 position = local_position;
	if (world_coordinates) {
		position = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	}
	ALBEDO = sample_3d_grid(position);
	ROUGHNESS = 1.0;
}
