#version 130
//#pragma include B:\Sharp.Engine\Sharp\Content\LightProcess.inc
#pragma enable blend
#pragma vertex
uniform mat4 camView;
uniform mat4 camProjection;
uniform mat4 model;
in vec3 vertex_position; // position of the vertices
in vec2 vertex_texcoord;
in vec3 vertex_barycentric; // barycentric coordinate inside the triangle

out vec3 v_barycentric; // barycentric coordinate inside the triangle

void main()
{
	gl_Position = camProjection * camView * model * vec4(vertex_position, 1.0f);
	v_barycentric = vertex_barycentric; // just pass it on
}

#pragma fragment

in vec3 v_barycentric; // barycentric coordinate inside the triangle
uniform float removeDiagonalEdges;
out vec4 frag_color;
void main()
{
	vec3 deltas = fwidth(v_barycentric);
	vec3 barys = smoothstep(vec3(0), 1.5f*deltas,  v_barycentric);
	float tmp = min(barys.y, barys.z);
	float minBary =removeDiagonalEdges*tmp + min(barys.x,tmp)*(1f- removeDiagonalEdges);
	frag_color = vec4(vec3(1.0f),1f-minBary);
}