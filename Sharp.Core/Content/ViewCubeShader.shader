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


		uniform sampler2D MyTexture;
		uniform sampler2D CubeTex;
		uniform sampler2D mask;
		//TODO: semi-transparent when not hovered over?
		uniform vec4 xColor;
		uniform vec4 yColor;
		uniform vec4 zColor;
		uniform vec4 hoverOverColor;
            in vec2 v_texcoord;
            out vec4 frag_color;
            void main(void)
            {
		 vec4 maskTex=texture(mask,v_texcoord.xy);
            vec4 texColor=texture(MyTexture,v_texcoord.xy);
			vec4 cubeColor=mix(mix(mix(texture(CubeTex,v_texcoord.xy), xColor,maskTex.x),yColor,maskTex.y),zColor,maskTex.z);
			  frag_color =mix(cubeColor, texColor,enablePicking);
            }
