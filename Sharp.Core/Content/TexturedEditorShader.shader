#version 130

#pragma vertex
            in vec3 vertex_position;
			in vec2 vertex_texcoord;
			out vec2 v_texcoord;
			uniform mat4 model;

            void main(void)
            {
				v_texcoord = vertex_texcoord;
                gl_Position = model*vec4(vertex_position, 1.0);
            }

		#pragma fragment


		uniform sampler2D tex;
		uniform vec4 tint;
            in vec2 v_texcoord;
            out vec4 frag_color;



            void main(void)
            {
            vec4 texColor= texture(tex,v_texcoord.xy);
	         frag_color =texColor;
            }
