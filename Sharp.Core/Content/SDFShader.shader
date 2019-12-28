
#version 130

#pragma vertex

in vec3 vertex_position;
in vec2 vertex_texcoord;
out vec2 v_texCoord;
uniform mat4 model;

void main(void) {
    v_texCoord =vertex_texcoord;
	gl_Position = model*vec4(vertex_position, 1.0);
}

#pragma fragment

//#ifdef GL_ES
//precision mediump float;
//#endif

in vec2 v_texCoord;
out vec4 frag_color;
uniform sampler2D msdf;
uniform vec4 color;

void main() {
    frag_color =vec4(color.rgb,color.a*texture2D(msdf, v_texCoord).a);// vec4(color.rgb,step(180.0f/255.0f,texture2D(msdf, v_texCoord).a));
	}
