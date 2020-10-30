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
		uniform vec2 camNearFar;
		uniform sampler2D MyTexture;
		uniform vec2 viewPort;
		uniform vec4 selectedIdColor;
            in vec2 uv;
            out vec4 frag_color;

			vec4 GatherAlpha(sampler2D tex, vec2 uv,ivec2 offset){
				float r1=textureOffset(tex,uv,ivec2(0,0)+offset).a;
				float r2=textureOffset(tex,uv,ivec2(1,0)+offset).a;
				float r3=textureOffset(tex,uv,ivec2(0,1)+offset).a;
				float r4=textureOffset(tex,uv,ivec2(1,1)+offset).a;
			return vec4(r1,r2,r3,r4);
			}
			vec4 GatherRed(sampler2D tex, vec2 uv,ivec2 offset){
				float r1=textureOffset(tex,uv,ivec2(0,0)+offset).r;
				float r2=textureOffset(tex,uv,ivec2(1,0)+offset).r;
				float r3=textureOffset(tex,uv,ivec2(0,1)+offset).r;
				float r4=textureOffset(tex,uv,ivec2(1,1)+offset).r;
			return vec4(r1,r2,r3,r4);
			}
		float linearize_depth(float d,float zNear,float zFar)
{
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}
            void main(void)
            {
            // Generate outline by comparing IDs between current pixel and surrounding
// pixels. This will collect 4x4 IDs but we will only be using the upper
// 3x3 taps.
/*vec4 id0 = GatherAlpha(MyTexture, uv, ivec2(-2, -2));
vec4 id1 = GatherAlpha(MyTexture, uv, ivec2( 0, -2));
vec4 id2 = GatherAlpha(MyTexture, uv, ivec2(-2,  0));
vec4 id3 = GatherAlpha(MyTexture, uv, ivec2( 0,  0));

// Throw away unused taps and merge into two float4 and a center tap
id2.xw = id1.xy;
float id_center = id3.w;
id3.w = id0.y;

// Count ID misses and average together. a becomes our alpha of the outline.
const float avg_scalar = 1.f / 8.f;
//float a = dot(vec4(id2 != id_center), vec4(avg_scalar));
//a += dot(vec4(id3 != id_center), vec4(avg_scalar));
float a=clamp(float(id_center!=id3)+float(id_center!=id2),0,1);*/

float a=0f;
vec4 currColor=texture(MyTexture, uv);
vec4 finalColor=vec4(0,0,0,0);
if(all(equal(currColor.rgb,vec3(0,0,1)))){
		finalColor=outline_color;
		a=0.75f;
	}
	
	/*else
	{
	float image_border=0f;
        for (int i = -w; i <= +w; i++)
        {
            for (int j = -w; j <= +w; j++)
            {
                if ((i == 0 && j == 0) || a==1f)
                {
                    continue;
                }

                vec2 offset = vec2(i, j) * size;
				vec2 totalUV=uv+offset;
                // and if one of the pixel-neighbor is black (we are on the border)
                if (texture(MyTexture,totalUV).a< 0.00001f)
                {
                    a = 1f;
                }
				if(totalUV.x>0.99999f || totalUV.y>0.99999f || totalUV.x<0.00001f || totalUV.y<0.00001f)
				image_border=1f;
            }
        }
		a=image_border==1f ? 1f : a;
	}*/

//frag_color =vec4(linearize_depth(texture(SceneDepthTex,res).r, camNearFar.x, camNearFar.y));
frag_color=(finalColor*a);//vec4(texture(MyTexture,uv).rgb,1);//
            }

