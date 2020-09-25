#version 130

#pragma vertex
in vec2 vertex_position;
uniform mat4 camOrtho;

void main() {
	gl_Position = camOrtho * vec4(vertex_position, 0.0, 1.0);
}
#pragma fragment
uniform vec4 color;
out vec4 output_color;

void main(void)
{
	output_color = color;
};
