#version 130

#pragma vertex
in vec3 vertex_position;
uniform mat4 camView;
uniform mat4 model;
uniform mat4 camProjection;

void main() {
	gl_Position = camProjection*camView *model* vec4(vertex_position, 1.0);
}

#pragma fragment
out vec4 output_color;

void main(void)
{
	output_color = vec4(1,1,1,1);
};
