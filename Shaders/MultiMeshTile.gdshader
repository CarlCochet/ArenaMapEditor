shader_type canvas_item;
render_mode blend_mix;

uniform sampler3D atlas_tex : filter_linear, repeat_disable;
uniform float total_layers = 1.0;
uniform float highlight_alpha = 1.0;

varying vec4 _instance_data;
varying float _v_span;

void vertex() {
	_instance_data = INSTANCE_CUSTOM;
	_v_span = COLOR.a;
	COLOR = vec4(COLOR.rgb, 1.0);
}

void fragment() {
	float layer = (_instance_data.x + 0.5) / total_layers;
	float u = _instance_data.y + UV.x * _instance_data.w;
	float v = _instance_data.z + (1.0 - UV.y) * _v_span;

	vec4 tex_color = texture(atlas_tex, vec3(u, v, layer));
	tex_color.a *= highlight_alpha;
	COLOR *= tex_color;
}
