//https://developer.download.nvidia.com/SDK/9.5/Samples/DEMOS/OpenGL/src/cg_VolumeLine/docs/VolumeLine.pdf
#version 130

#pragma vertex
in vec2 vertex_position;
in vec2 dir;
in float miter;
uniform mat4 camOrtho;
uniform float width;

void main() {
float thickness =width/ 2.0f;
	//push the point along its normal by half thickness
	vec2 n =normalize(dir);
	vec2 p = vertex_position.xy + vec2(vec2(n.y,-n.x) * thickness * miter);
	gl_Position = camOrtho * vec4(p, 0.0, 1.0);}
/*
*/
#pragma fragment
uniform vec4 color;
out vec4 output_color;

void main(void)
{
	output_color = color;
};
