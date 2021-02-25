#version 130

#pragma vertex
in vec3 vertex_position;
in vec4 vertex_color;
out vec4 v_color;
uniform mat4 camView;
uniform mat4 camProjection;
uniform mat4 model;
uniform vec2 viewPort;
uniform vec4 highlightColor;
uniform float enableHighlight;
//uniform vec3 axis;
mat4 mix(mat4 m1, mat4 m2, float factor) {
	return (m1 * (1.0 - factor)) + (m2 * factor);
}
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
				gl_Position = camProjection * camView *model* vec4(vertex_position.xyz, 1.0);
				v_color = vertex_color;
			}

		#pragma fragment
			in vec4 v_color;
            out vec4 frag_color;
			uniform vec4 color;
			uniform vec4 highlightColor;
			uniform float enableHighlight;

            void main(void)
            {
				vec4 highlight = highlightColor * enableHighlight;

	         frag_color = highlight + vec4(v_color.rgb, 1) * (1 - highlight.a);
            }
