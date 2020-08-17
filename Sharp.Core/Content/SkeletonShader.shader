#version 330

#pragma vertex
            in vec3 vertex_position;

            uniform mat4 mvp_matrix;

            void main(void)
            {
                gl_Position = mvp_matrix * vec4(vertex_position, 1.0);
            }

		#pragma fragment
            out vec4 frag_color;



            void main(void)
            {
	         frag_color =vec4(0.5,0.7,0.5,1);
            }