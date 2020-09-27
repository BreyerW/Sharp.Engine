#version 130
//#pragma include B:\Sharp.Engine\Sharp\Content\LightProcess.inc
#pragma vertex
in vec3 vertex_position;
in vec2 vertex_texcoord;
out vec2 uv;
uniform mat4 camOrtho;
uniform mat4 model;
uniform vec2 len;

            void main()
{
    gl_Position = vec4(vertex_position.xy,0, 1.0);
	uv=vertex_texcoord;
	//uv.y=-vertex_texcoord.y;
}

		#pragma fragment
		in vec2 uv;
		out vec4 fragColor;
		uniform sampler2D renderTexture;
		
		void main()
{
    fragColor =texture(renderTexture,uv);//vec4(uv,0f,1f);
}