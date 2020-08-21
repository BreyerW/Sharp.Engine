#version 130

#pragma vertex
            in vec3 vertex_position;
			uniform mat4 model;

            void main(void)
            {
                gl_Position = model*vec4(vertex_position, 1.0);
            }

		#pragma fragment

		uniform vec4 color;
            out vec4 frag_color;



            void main(void)
            {
	         frag_color =color;
            }
