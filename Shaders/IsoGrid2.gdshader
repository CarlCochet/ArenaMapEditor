shader_type canvas_item;

uniform vec2 mouse_position;
uniform float zoom;

uniform float line_intensity = 5.0;
uniform float border_size = 0.468;
uniform float line_blur = 0.2;
uniform float grid_scale: hint_range(0.1, 2.0) = 1.17;
uniform float elevation = 0.0;

const float large_grid_scale = 18.0;
const float mouse_influence_radius = 600.0;
const float min_grid_alpha = 0.1;
const float zoom_fade_start = 0.8;
const float zoom_fade_end = 0.1;

varying vec2 global_coords;

void vertex(){
	global_coords = (MODEL_MATRIX * vec4(VERTEX, 1.0, 1.0)).xy;
}

vec2 a_grid(vec2 pos){
	return vec2(mod(pos.x, 1.0), mod(pos.y * 2.0, 1.0));
}

vec2 b_grid(vec2 pos){
	return vec2(mod(pos.x - 0.5, 1.0), mod((pos.y * 2.0) - 0.5, 1.0));
}

float small_los(vec2 pos){
	vec2 abspos = abs(pos - vec2(0.5));
	return smoothstep(border_size, border_size + line_blur, abspos.x + abspos.y);
}

float elliptical_distance(vec2 p1, vec2 p2){
	vec2 diff = p1 - p2;
	diff.y *= 2.0;
	return length(diff);
}

void fragment() {
	vec2 coords = global_coords.xy * grid_scale * 0.01 * 0.05555;
	coords.y += 0.014;
	coords.y += elevation * 0.01;

	float dist_to_mouse = elliptical_distance(global_coords, mouse_position);
	float mouse_factor = smoothstep(mouse_influence_radius, 0.0, dist_to_mouse);

	float alos = small_los(a_grid(coords));
	float blos = small_los(b_grid(coords));

	float small_grid = min(alos, blos);
	float final_small = mix(min_grid_alpha, 1.0, mouse_factor) * small_grid;

	COLOR = vec4(pow(final_small, 1.0 / line_intensity));
}