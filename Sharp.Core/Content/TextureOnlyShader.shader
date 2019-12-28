#version 130

#pragma vertex
            in vec3 vertex_position;
            in vec2 vertex_texcoord;
            //in vec3 vertex_normal;

            uniform mat4 camView;
            uniform mat4 camProjection;
			uniform mat4 model;

            out vec2 out_uv;
            //out vec3 out_normal;

            void main(void)
            {
                out_uv=vertex_texcoord;
                //out_normal=vertex_normal;
                gl_Position = camProjection*camView*model * vec4(vertex_position, 1.0);
            }

		#pragma fragment


		uniform sampler2D MyTexture;


            in vec2 out_uv;
            out vec4 frag_color;


            void main(void)
            {
            vec4 texColor= texture(MyTexture,out_uv.xy);
	         frag_color =texColor;
            }
