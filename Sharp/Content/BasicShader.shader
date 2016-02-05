#version 330

#pragma vertex
            in vec3 vertex_position;
            in vec4 vertex_color;
            in vec2 vertex_texcoord;
            in vec3 vertex_normal;

            uniform mat4 camera;
			uniform mat4 model;

            out vec2 out_uv;
            out vec4 color;
            out vec3 fragVert;
            out vec3 out_normal;

            void main(void)
            {
            fragVert=vertex_position;
                color =vertex_color;
                out_uv=vertex_texcoord;
                out_normal=vertex_normal;
                gl_Position = camera*model * vec4(vertex_position, 1.0);
            }

		#pragma fragment

		#define PHONG_LIGHT

		#pragma include B:\Sharp_kopia\Sharp\Content\LightProcess.inc

		uniform mat4 model;
		uniform sampler2D MyTexture;
		uniform Light lights[10];

		in vec4 color;
            in vec2 out_uv;
            in vec3 fragVert;
            in vec3 out_normal;
            out vec4 frag_color;


            void main(void)
            {
	         frag_color =ApplyLight(model,lights[0],out_normal,fragVert)* texture(MyTexture,out_uv.xy);
	         //frag_color =color;
            }
