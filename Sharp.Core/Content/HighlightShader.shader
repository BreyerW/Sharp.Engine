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
		uniform sampler2D SelectionScene;
		uniform sampler2D SelectionDepthTex;
		uniform sampler2D SceneDepthTex;
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

int w = 3;
float a=0f;
float smallest_distance=3.402823466e+38f;
vec2 depth_offset=vec2(0);
vec2 size = 1.0f / textureSize(MyTexture, 0);
vec2 realsize = textureSize(MyTexture, 0);
vec4 currColor=texture(MyTexture, uv);
//if(all(equal(currColor.rgb,vec3(1,1,0)))){frag_color=vec4(0,0,0,0); return; }
    // if the pixel is black (we are on the silhouette)
    if (currColor.r==0/*texture(MyTexture, uv).a < 0.00001f*/)
    {
	//w-=2;
        for (int i = -w; i <= +w; i++)
        {
            for (int j = -w; j <= +w; j++)
            {
                if ((i == 0 && j == 0)||smallest_distance<2)
                {
                    continue;
                }

                vec2 offset = vec2(i, j) * size;

                // and if one of the pixel-neighbor is white (we are on the border)
                if (texture(MyTexture, uv+ offset).r!=0/*texture(MyTexture, uv + offset).a> 0.00001f*/)
                {
                    a = 1f;
					
					float l=length(vec2(i, j));
					if(l<smallest_distance){
						depth_offset=offset;
						smallest_distance=l;
					}
                }
            }
        }
    }
	else if(currColor.r==1){
		w=3;
		for (int i = -w; i <= +w; i++)
        {
            for (int j = -w; j <= +w; j++)
            {
               // if ((i == 0 && j == 0)||smallest_distance<2)
                {
                 //   continue;
                }

                vec2 offset = vec2(i, j) * size;

                // and if one of the pixel-neighbor is white (we are on the border)
                if (all(notEqual(texture(MyTexture, uv+ offset).rr,vec2(0,1f)))/*texture(MyTexture, uv + offset).a> 0.00001f*/)
                {
                    a = 1f;
					
					float l=length(vec2(i, j));
					if(l<smallest_distance){
						depth_offset=offset;
						smallest_distance=l;
					}
                }
            }
        }

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


// To allow outline to bleed over objects and combat TAA jittering artifacts
// sample depth of selection buffer in a 4x4 neighborhood and pick closest
// depth.
if(texture(MyTexture, uv+depth_offset).r==1)
{
vec4 dtap0 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2(-2, -2));
vec4 dtap1 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2( 0, -2));
vec4 dtap2 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2(-2,  0));
vec4 dtap3 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2( 0,  0));
float d0 = min(dtap0.x, min(dtap0.y, min(dtap0.z, dtap0.w)));
float d1 = min(dtap1.x, min(dtap1.y, min(dtap1.z, dtap1.w)));
float d2 = min(dtap2.x, min(dtap2.y, min(dtap2.z, dtap2.w)));
float d3 = min(dtap3.x, min(dtap3.y, min(dtap3.z, dtap3.w)));
float d = min(d0, min(d1, min(d2, d3)));

// Sample scene depth, scn_depth holds linear depth.
float scnd = texture(SceneDepthTex, uv+depth_offset).r;

// Linearize d and compare it with scene depth to determine if outline is
// behind an object.=
bool visible = d <=scnd;

// If outline is hidden, reduce its alpha value to 30%.
a *= visible ? 1.f : 0.33f;
}
//frag_color =vec4(linearize_depth(texture(SceneDepthTex,res).r, camNearFar.x, camNearFar.y));
frag_color=(outline_color*a);//vec4(texture(MyTexture,uv).rgb,1);//
            }


			/*//based on https://ourmachinery.com/post/borderland-part-3-selection-highlighting/


            void main(void)
            {

int w = 3;
float a=0f;
float smallest_distance=3.402823466e+38f;
vec2 depth_offset=vec2(0);
vec2 size = 1.0f / textureSize(SelectionScene, 0);
vec4 colorUnderMouse=texture(SelectionScene,mousePos/viewPort);

    // if the pixel is black (we are on the silhouette)
    if (/*texture(MyTexture, uv).a < 0.00001f*any(notEqual(texture(SelectionScene, uv).rgb,colorUnderMouse.rgb)))
    {
	//w-=2;
for (int i = -w; i <= +w; i++)
        {
            for (int j = -w; j <= +w; j++)
            {
                if ((i == 0 && j == 0) ||smallest_distance<2)
                {
                    continue;
                }

                vec2 offset = vec2(i, j)* size;
                // and if one of the pixel-neighbor is white (we are on the border)
                if (/*texture(MyTexture, uv + offset).a> 0.00001f* all(equal(colorUnderMouse,texture(SelectionScene, uv + offset))))
                {
                    a = 1f;
					float l=length(vec2(offset));
					if(l<smallest_distance){
						depth_offset=offset;
						smallest_distance=l;
					}
                }
            }
        }
    }
	else if (all(equal(texture(SelectionScene, uv).rgb,colorUnderMouse.rgb)))
	{
	w-=2;
	
        for (int i = -w; i <= +w; i++)
        {
            for (int j = -w; j <= +w; j++)
            {
                if ((i == 0 && j == 0) || a==1f)
                {
                    continue;
                }

                vec2 offset = ivec2(i, j) * size;
				vec2 totalUV=uv+(offset);
                // and if one of the pixel-neighbor is black (we are on the border)
                if (/*texture(MyTexture,totalUV).a< 0.00001f*any(notEqual(colorUnderMouse.rgb,texture(SelectionScene, uv + offset).rgb)))
                {
                    a = 1f;
					float l=length(vec2(offset));
					if(l<smallest_distance){
						depth_offset=offset;
						smallest_distance=l;
					}
                }
            }
        }
		
		/*if(a==0){
		w=5;
		float image_border=0f;
		for (int i = -w; i <= +w; i++)
        {
            for (int j = -w; j <= +w; j++)
            {
                if ((i == 0 && j == 0) || a==1f)
                {
                    continue;
                }

                vec2 offset = ivec2(i, j) * size;
				vec2 totalUV=uv+(offset);
                // and if one of the pixel-neighbor is black (we are on the border)
                if (/*texture(MyTexture,totalUV).a< 0.00001f*all(equal(vec3(0,0,0),texture(SelectionScene, uv + offset).rgb)))
                {
                    a = 1f;
					
                }
				if(totalUV.x>0.999f || totalUV.y>0.999f || totalUV.x<0.00001f || totalUV.y<0.00001f)
				image_border=1f;
            }
        }
		a=image_border==1f ? 1f : a;
		}*
	}


// To allow outline to bleed over objects and combat TAA jittering artifacts
// sample depth of selection buffer in a 4x4 neighborhood and pick closest
// depth.
vec4 dtap0 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2(-2, -2));
vec4 dtap1 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2( 0, -2));
vec4 dtap2 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2(-2,  0));
vec4 dtap3 =GatherRed(SelectionDepthTex, uv+depth_offset, ivec2( 0,  0));
float d0 = min(dtap0.x, min(dtap0.y, min(dtap0.z, dtap0.w)));
float d1 = min(dtap1.x, min(dtap1.y, min(dtap1.z, dtap1.w)));
float d2 = min(dtap2.x, min(dtap2.y, min(dtap2.z, dtap2.w)));
float d3 = min(dtap3.x, min(dtap3.y, min(dtap3.z, dtap3.w)));
float d = min(d0, min(d1, min(d2, d3)));

// Sample scene depth, scn_depth holds linear depth.
float scnd = texture(SceneDepthTex, uv+depth_offset).r;

// Linearize d and compare it with scene depth to determine if outline is
// behind an object.=
bool visible = d <=scnd;

// If outline is hidden, reduce its alpha value to 30%.
a *= visible ?1f:1f;
if(all(equal(colorUnderMouse.rgb,vec3(0,0,0)))) a=0;
//frag_color =vec4(linearize_depth(texture(SceneDepthTex,res).r, camNearFar.x, camNearFar.y));texelFetch(SelectionScene,ivec2(mousePos.xy),0)
frag_color=(outline_color*a);//vec4(texture(SelectionScene,uv).rgb*200,1);
            }
*/