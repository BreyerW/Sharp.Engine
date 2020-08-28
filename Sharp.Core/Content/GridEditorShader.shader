#version 140

#pragma vertex
in vec3 vertex_position;
in vec2 vertex_texcoord;
out vec2 v_texcoord;
out vec3 v_pos;
uniform mat4 camView;
uniform mat4 camProjection;
uniform mat4 model;
uniform vec2 viewPort;
uniform float len;

            void main(void)
            {
				vec4 pp =camProjection*camView*model* vec4(vertex_position.xz*len, vertex_position.y * len, 1.0);
				pp.z =1.0e-6f; //pp.w - 1.0e-6f; reverse-z or traditional z depth
				gl_Position = pp;
				v_texcoord = vertex_texcoord*len;
				v_pos = (model * vec4(vertex_position.xz * len, vertex_position.y * len,1.0f)).xyz;
            }

		#pragma fragment
			uniform mat4 camView;
			uniform mat4 camWorldView;
			uniform mat4 camProjection;
			uniform mat4 model;
			uniform float width;
			uniform vec4 color;
			uniform vec2 viewPort;
			uniform float len;
            out vec4 frag_color;
			in vec2 v_texcoord;
			in vec3 v_pos;
			vec3 getCameraPos() {
				return vec3(camWorldView[3][0], camWorldView[3][1], camWorldView[3][2]);
			}
			vec3 getCameraDir() {
				mat4 m = camWorldView;
				return vec3(m[2][0], m[2][1], m[2][2]);
			}
			float log10(float x) { return  (1f/log(10f))* log(x); }
			float saturate(float x) {
				return clamp(x, 0.0, 1.0);
			}
			vec2 saturate(vec2 x) {
				return clamp(x,vec2(0.0),vec2(1.0));
			}
			vec2 fmod(vec2 x, float y) {
				return x - y * trunc(x / y);
			}
			void main(void)
			{
				// UV is grid space coordinate of pixel.
				vec2 uv =abs(v_texcoord-vec2(len)/2f);

				// Find screen-space derivates of grid space. [1]
				vec2 dudv = vec2(length(vec2(dFdx(uv.x), dFdy(uv.x))),
					length(vec2(dFdx(uv.y), dFdy(uv.y))));

				// Define minimum number of pixels between cell lines before LOD switch should occur. 
				const float min_pixels_between_cells = 1.f;

				// Load cell size from tm_visual_grid_t, minimum size of a grid cell in world units 
				// that will be visualized. 
				float cs = 1f;

				// Calc lod-level [2].
				float lod_level = max(0, log10((length(dudv) * min_pixels_between_cells) / cs) + 1);
				float lod_fade = fract(lod_level);

				// Calc cell sizes for lod0, lod1 and lod2. 
				float lod0_cs = cs * pow(10, floor(lod_level));
				float lod1_cs = lod0_cs * 10.f;
				float lod2_cs = lod1_cs * 10.f;

				// Allow each anti-aliased line to cover up to 2 pixels. 
				dudv *= width;

				// Calculate unsigned distance to cell line center for each lod [3]
				vec2 lod0_cross_a = 1.f - abs(saturate(fmod(uv, lod0_cs) / dudv) * 2 - 1.f);
				// Pick max of x,y to get a coverage alpha value for lod
				float lod0_a = max(lod0_cross_a.x, lod0_cross_a.y);
				vec2 lod1_cross_a = 1.f - abs(saturate(fmod(uv, lod1_cs) / dudv) * 2 - 1.f);
				float lod1_a = max(lod1_cross_a.x, lod1_cross_a.y);
				vec2 lod2_cross_a = 1.f - abs(saturate(fmod(uv, lod2_cs) / dudv) * 2 - 1.f);
				float lod2_a = max(lod2_cross_a.x, lod2_cross_a.y);

				// Load sRGB colors from tm_visual_grid_t (converted into 0-1 range) 
				vec4 thin_color = vec4(1f, 1f, 1f, 0.5f);
				vec4 thick_color = vec4(1f, 1f, 1f, 0.9f);

				// Blend between falloff colors to handle LOD transition [4]
				vec4 c = lod2_a > 0 ? thick_color : lod1_a > 0 ? mix(thick_color, thin_color, lod_fade) : thin_color;
				vec3 cam_pos = getCameraPos();
				// Calculate opacity falloff based on distance to grid extents and gracing angle. [5]
				vec3 view_dir = normalize((camView * vec4(0f, 0f, 1f, 1f)).xyz);
				//float op_gracing = 1.f - pow(1.f - abs(dot(view_dir, normalize(camView * model * vec4(0f, 0f, 1f, 1f)).xyz)), 16);
				float op_distance =1.f- saturate(distance(cam_pos.xz,v_pos.xz)/(abs(cam_pos.y)*1000f * 0.005f +1000f*0.25f)); //1000f zfar of camera
				float op = op_distance;

				// Blend between LOD level alphas and scale with opacity falloff. [6]
				c.a *= (lod2_a > 0 ? lod2_a : lod1_a > 0 ? lod1_a : (lod0_a * (1f - lod_fade))) * op;
				
				frag_color = c;

			}