#version 130

#pragma vertex
            in vec3 vertex_position;
			in vec2 vertex_texcoord;
			out vec2 v_texcoord;
            uniform mat4 camView;
            uniform mat4 camOrtho;
			uniform mat4 model;

            void main(void)
            {
				v_texcoord = vertex_texcoord;
                gl_Position =model* vec4(vertex_position, 1.0);
			 }

		#pragma fragment
            in vec2 v_texcoord;
			in float switchColor;
            out vec4 frag_color;
            void main(void)
            {
			frag_color = vec4(0,0,1,1);
			}
