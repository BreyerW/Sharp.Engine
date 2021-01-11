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

		uniform sampler2D CubeTex;
		uniform sampler2D mask;
		uniform vec4 xColor;
		uniform vec4 yColor;
		uniform vec4 zColor;
		uniform vec4 edgeColor;
		uniform vec4 faceColor;
		uniform float isHovered;
            in vec2 v_texcoord;
            out vec4 frag_color;
            void main(void)
            {
			float a =0.33f+0.67f*isHovered;
		 vec4 maskTex=texture(mask,v_texcoord.xy);
			vec4 cubeColor=mix(mix(mix(mix(edgeColor,faceColor, texture(CubeTex,v_texcoord.xy).r), xColor,maskTex.x),yColor,maskTex.y),zColor,maskTex.z);
			  frag_color =vec4(cubeColor.rgb,1);//float(any(bvec2(enablePicking,isHovered)))

			  }
