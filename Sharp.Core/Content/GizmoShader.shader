#version 130

#pragma vertex
in vec3 vertex_position;
uniform mat4 camView;
uniform mat4 camProjection;
uniform mat4 model;
uniform vec2 viewPort;
uniform vec2 len;
//uniform vec3 axis;

            void main(void)
            {
				//billboard matrix along specified axis 
				/*total[0][0] =mix(1.0, total[0][0], 1 - axis.x);
				total[0][1] = mix(0.0, total[0][1], 1 - axis.x);
				total[0][2] = mix(0.0, total[0][2], 1 - axis.x);

				total[1][0] = mix(0.0, total[1][0],1- axis.y);
				total[1][1] = mix(1.0, total[1][1], 1 - axis.y);
				total[1][2] = mix(0.0, total[1][2], 1 - axis.y);

				total[2][0] = mix(0.0, total[2][0], 1 - axis.z);
				total[2][1] = mix(0.0, total[2][1], 1 - axis.z);
				total[2][2] = mix(1.0, total[2][2], 1 - axis.z);
				*/
				gl_Position = camProjection * camView *model* vec4(vertex_position.xy* len,vertex_position.z*len.y, 1.0);
            }

		#pragma fragment

            out vec4 frag_color;
			uniform vec4 color;

            void main(void)
            {
	         frag_color =color;
            }
