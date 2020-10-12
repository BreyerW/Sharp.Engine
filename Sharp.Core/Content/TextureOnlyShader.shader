#version 130

#pragma vertex
            in vec3 vertex_position;
			in vec2 vertex_texcoord;
			out vec2 v_texcoord;
            in vec3 vertex_normal;

            uniform mat4 camView;
            uniform mat4 camProjection;
			uniform mat4 model;
            //out vec3 out_normal;

            void main(void)
            {
				v_texcoord = vertex_texcoord;
                //out_normal=vertex_normal;
                gl_Position = camProjection*camView*model*vec4(vertex_position, 1.0);
            }

		#pragma fragment


		uniform sampler2D MyTexture;


            in vec2 v_texcoord;
            out vec4 frag_color;


            void main(void)
            {
            vec4 texColor= texture(MyTexture,v_texcoord.xy);
	         frag_color =EnablePicking(texColor);
            }
