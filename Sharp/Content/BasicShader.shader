#version 330

#pragma vertex
            in vec3 vertex_position;
            in vec4 vertex_color;
            in vec2 vertex_texcoord;
            in vec3 vertex_normal;

            uniform mat4 camView;
            uniform mat4 camProjection;
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
                gl_Position = camProjection*camView*model * vec4(vertex_position, 1.0);
            }

		#pragma fragment

		#define PHONG_LIGHT

		#pragma include B:\Sharp_kopia\Sharp\Content\LightProcess.inc

		uniform mat4 model;
		uniform mat4 camView;
		uniform mat4 camProjection;
		uniform sampler2D MyTexture;
		uniform Light lights[10];

		//float Shininess=80.0;
 		//vec3 SpecularColor=;

		in vec4 color;
            in vec2 out_uv;
            in vec3 fragVert;
            in vec3 out_normal;
            out vec4 frag_color;


            void main(void)
            {
            vec4 texColor= texture(MyTexture,out_uv.xy);
	         frag_color =ApplyLight(model,camView,lights[0],texColor,out_normal,fragVert,float(80),vec3(1,1,1));
	         //frag_color =color;
            }
