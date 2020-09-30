//based on https://medium.com/@bgolus/the-quest-for-very-wide-outlines-ba82ed442cd9
#version 130

#pragma vertex
in vec3 vertex_position;
in vec2 vertex_texcoord;

uniform mat4 camView;
            uniform mat4 camProjection;
			uniform mat4 model;

out ivec2 position;
out vec2 uv;

void main() {
	gl_Position = camProjection*camView*model*vec4(vertex_position, 1.0);
	uv=vertex_texcoord;
	position=ivec2((camProjection*camView*model*vec4(vertex_position, 1.0)).xy);
}

#pragma fragment
#define SNORM16_MAX_FLOAT_MINUS_EPSILON float(float(32768-2) / float(32768-1))
        #define FLOOD_ENCODE_OFFSET vec2(1.0f, SNORM16_MAX_FLOAT_MINUS_EPSILON)
        #define FLOOD_ENCODE_SCALE vec2(2.0f, 1.0f + SNORM16_MAX_FLOAT_MINUS_EPSILON)

        #define FLOOD_NULL_POS -1.0f
        #define FLOOD_NULL_POS_FLOAT2 vec2(FLOOD_NULL_POS, FLOOD_NULL_POS)

in vec2 uv;
in ivec2 position;
out vec4 output_color;
uniform sampler2D renderTex;

void main(void)
{
mat3 values;
ivec2 texelSize=textureSize(renderTex,0);
vec4 TexelSize=vec4(1.0f/texelSize,texelSize);
                for(int u=0; u<3; u++)
                {
                    for(int v=0; v<3; v++)
                    {
                        ivec2 sampleUV = clamp(ivec2(position) + ivec2(u-1, v-1), ivec2(0,0), ivec2(TexelSize.zw) - 1);
                        values[u][v] = texelFetch(renderTex,sampleUV, 0).r;
                    }
                }

                // calculate output position for this pixel
                vec2 outPos = vec2(position) * abs(TexelSize.xy) * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET;

                // interior, return position
                if (values[0][0] > 0.99f){

                    output_color =vec4(outPos,0,1);
					return;
					}
                // exterior, return no position
                if (values[0][0] < 0.01f){
                   output_color =vec4(FLOOD_NULL_POS_FLOAT2,0,1);
				   return;
					}
                // sobel to estimate edge direction
               vec2 dir = -vec2(
                    values[0][0] + values[0][1] * 2.0f + values[0][2] - values[2][0] - values[2][1] * 2.0f - values[2][2],
                    values[0][0] + values[1][0] * 2.0f + values[2][0] - values[0][2] - values[1][2] * 2.0f - values[2][2]
                    );

                // if dir length is small, this is either a sub pixel dot or line
                // no way to estimate sub pixel edge, so output position
                if (abs(dir.x) <= 0.005f && abs(dir.y) <= 0.005f){

                    output_color =vec4( outPos,0,1);
					return;
					}
                // normalize direction
                dir = normalize(dir);

                // sub pixel offset
                vec2 offset = dir * (1.0f - values[0][0]);

                // output encoded offset position
                output_color =vec4((position.xy + offset) * abs(TexelSize.xy) * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET,0,1);
       
};
