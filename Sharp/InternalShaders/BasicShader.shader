#version 330

#pragma vertex
            attribute vec3 vertex_position;
            attribute vec4 vertex_color;
           attribute vec2 vertex_texcoord;

            uniform mat4 mvp_matrix;
            varying vec2 out_uv;
            attribute vec4 color;

            void main(void)
            {
                color =vertex_color;
                out_uv=vertex_texcoord;
                gl_Position = mvp_matrix * vec4(vertex_position, 1.0);
            }

		#pragma fragment
            out vec4 frag_color;

            in vec4 color;
            in vec2 out_uv;

            uniform sampler2D MyTexture;

            void main(void)
            {
	         frag_color = texture2D(MyTexture,out_uv.xy);
	         frag_color =float4(0,0,0,0);
            }