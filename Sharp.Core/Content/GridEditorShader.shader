#version 140

#pragma vertex
in vec3 vertex_position;
in vec2 vertex_texcoord;
out vec2 v_texcoord;
uniform mat4 camView;
uniform mat4 camProjection;
uniform mat4 model;
uniform vec2 viewPort;
uniform float len;

void main(void)
{
	vec4 pp = camProjection * camView * model * vec4(vertex_position * len, 1.0);
	//depth=computeDepth(vec3(vertex_position.xz*len, vertex_position.y * len));
	//pp.w = pp.w - 1.0e-6f; //reverse-z or traditional z depth
	gl_Position = pp;
	v_texcoord = vertex_texcoord * len;
}

#pragma fragment
uniform mat4 camView;
uniform mat4 camWorldView;
uniform mat4 camProjection;
uniform mat4 model;
uniform float width;
uniform vec4 color;
uniform vec2 viewPort;
uniform float len;
uniform vec2 camNearFar;
out vec4 frag_color;
in vec2 v_texcoord;

/*float computeDepth(vec3 pos) {
	vec4 clip_space_pos = camProjection * camView * model * vec4(pos.xyz, 1.0);
	float clip_space_depth = clip_space_pos.z / clip_space_pos.w;

	float far = gl_DepthRange.far;
	float near = gl_DepthRange.near;

	float depth = (((far - near) * clip_space_depth) + near + far) / 2.0;

	return depth;
}

vec3 getCameraDir() {
	mat4 m = camWorldView;
	return vec3(m[2][0], m[2][1], m[2][2]);
}*/
vec3 getCameraPos() {
	return vec3(camWorldView[3][0], camWorldView[3][1], camWorldView[3][2]);
}
float log10(float x) { return  (1f / log(10f)) * log(x); }
float saturate(float x) {
	return clamp(x, 0.0, 1.0);
}
vec2 saturate(vec2 x) {
	return clamp(x, vec2(0.0), vec2(1.0));
}
vec2 fmod(vec2 x, float y) {
	return x - y * trunc(x / y);
}
void main(void)
{
	// UV is grid space coordinate of pixel.
	vec2 uv = v_texcoord;

	// Find screen-space derivates of grid space. [1]
	vec2 dudv = vec2(length(vec2(dFdx(uv.x), dFdy(uv.x))),
		length(vec2(dFdx(uv.y), dFdy(uv.y))));

	// Define minimum number of pixels between cell lines before LOD switch should occur. 
	const float min_pixels_between_cells = 3f;

	// Load cell size from tm_visual_grid_t, minimum size of a grid cell in world units 
	// that will be visualized. 
	float cs = 1f;

	// Calc lod-level [2].
	float lod_level = max(0, log10((length(dudv) * min_pixels_between_cells) / cs) + 1);
	float lod_fade = fract(lod_level);

	// Calc cell sizes for lod0, lod1 and lod2. 
	float lod0_cs = cs * pow(10, floor(lod_level));
	float lod1_cs = lod0_cs * 10.f;
	float lod2_cs = lod1_cs * 10.f;

	// Allow each anti-aliased line to cover up to 2 pixels. 
	dudv *= width;

	// Calculate unsigned distance to cell line center for each lod [3]
	vec2 lod0_cross_a = 1.f - abs(saturate(mod(uv, lod0_cs) / dudv) * 2 - 1.f);
	// Pick max of x,y to get a coverage alpha value for lod
	float lod0_a = max(lod0_cross_a.x, lod0_cross_a.y);
	vec2 lod1_cross_a = 1.f - abs(saturate(mod(uv, lod1_cs) / dudv) * 2 - 1.f);
	float lod1_a = max(lod1_cross_a.x, lod1_cross_a.y);
	vec2 lod2_cross_a = 1.f - abs(saturate(mod(uv, lod2_cs) / dudv) * 2 - 1.f);
	float lod2_a = max(lod2_cross_a.x, lod2_cross_a.y);

	// Load sRGB colors from tm_visual_grid_t (converted into 0-1 range) 
	vec4 thin_color = color * 0.5f;
	vec4 thick_color = color * 0.9f;
	vec4 thick_color2 = mix(thick_color, vec4(1, 0.25f, 0.25f, 1), float(abs(uv.x / dudv.x) < 1f && abs(uv.y / dudv.y) > 1f && abs(uv.y / dudv.y) < len / abs(dudv.y)));
	thick_color2 = mix(thick_color2, vec4(0.26f, 0.6f, 1, 1), float(abs(uv.y / dudv.y) < 1f && abs(uv.x / dudv.x) > 1f && abs(uv.x / dudv.x) < len / abs(dudv.x)));

	// Blend between falloff colors to handle LOD transition [4]
	//equivalent to lod2_a > 0 ? thick_color : lod1_a > 0 ? mix(thick_color, thin_color, lod_fade) : thin_color; but 0 branches
	vec4 c = mix(mix(thin_color, mix(thick_color, thin_color, lod_fade), float(lod1_a > 0)), thick_color2, float(lod2_a > 0));



	vec3 cam_pos = getCameraPos();
	// Calculate opacity falloff based on distance to grid extents and gracing angle. [5]
	//vec3 view_dir = normalize(getCameraDir().xyz);
	//float op_gracing = 1.f - pow(1.f - abs(dot(view_dir, vec3(camView[1][0], camView[1][1], camView[1][2]))), 16);
	float op_distance = 1.f - saturate(distance(cam_pos.xz, uv) / (abs(cam_pos.y) * camNearFar.y * 0.005f + camNearFar.y * 0.25f)); //1000f zfar of camera
	//float op_distance = (1.f - saturate(length(uv) / len));
	float op = op_distance;

	// Blend between LOD level alphas and scale with opacity falloff. [6]
	//(lod2_a > 0 ? lod2_a : lod1_a > 0 ? lod1_a : (lod0_a * (1f - lod_fade))) * op
	c.a *= mix(mix((lod0_a * (1f - lod_fade)), lod1_a, float(lod1_a > 0)), lod2_a, float(lod2_a > 0));// *op;

	frag_color = c;

}