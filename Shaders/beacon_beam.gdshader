// Feixe de luz do destino de entrega: visível de longe, atravessa a neblina.
// Pensado para um CylinderMesh de altura 1 (escalado no nó).
shader_type spatial;
render_mode unshaded, cull_disabled, blend_add, depth_draw_never, fog_disabled;

uniform vec3 beam_color : source_color = vec3(0.4, 1.0, 0.55);
uniform float intensity = 1.0;

varying float height01;

void vertex() {
	height01 = clamp(VERTEX.y + 0.5, 0.0, 1.0);
}

void fragment() {
	float fade = 1.0 - height01;
	float pulse = 0.75 + 0.25 * sin(TIME * 3.0);
	float facing = abs(dot(NORMAL, VIEW));
	ALBEDO = beam_color * intensity * pulse * facing * (0.15 + 0.85 * fade);
}
