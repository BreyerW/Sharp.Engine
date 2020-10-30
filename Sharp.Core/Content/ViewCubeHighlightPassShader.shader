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
		uniform vec4 hoveredColorId;
            in vec2 v_texcoord;
            out vec4 frag_color;
            void main(void)
            {
vec4 idMap=texture(MyTexture,v_texcoord.xy);
float condition=float(all(equal(hoveredColorId,vec4(0,0,0,1))));
            vec4 texColor=mix(vec4(1,1,1,1),vec4(0,0,1,1),float(all(equal(idMap.rgb,hoveredColorId.rgb))));
			  frag_color = texColor;//float(any(bvec2(enablePicking,isHovered)))
            }
