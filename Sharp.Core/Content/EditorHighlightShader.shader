//based on https://ourmachinery.com/post/borderland-part-3-selection-highlighting/

#version 130

#pragma vertex
            in vec3 vertex_position;
			in vec2 vertex_texcoord;
			out vec2 uv;

            void main(void)
            {
				uv = vertex_texcoord;
                gl_Position = vec4(vertex_position, 1.0);
            }

		#pragma fragment

		uniform vec4 outline_color;
		uniform sampler2D MyTexture;
		uniform vec4 hoverIdColor;
            in vec2 uv;
            out vec4 frag_color;

            void main(void)
            {
			float a=0f;
    // if the pixel is black (we are on the silhouette)
    if (all(equal(texture(MyTexture, uv).rgb,hoverIdColor.rgb)))
    {
	a=0.55f;
	}
if(round(hoverIdColor.x*255)>26 && round(texture(MyTexture, uv).x*255)<27) a=0f;//use this trick to add border to viewcube only?
frag_color=(outline_color*a)*hoverIdColor.a;//vec4(texture(MyTexture,uv).rgb*10,1);//
            }