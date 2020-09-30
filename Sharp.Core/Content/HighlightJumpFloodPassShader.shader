//based on https://medium.com/@bgolus/the-quest-for-very-wide-outlines-ba82ed442cd9
#version 130

#pragma vertex
in vec3 vertex_position;

uniform mat4 camView;
            uniform mat4 camProjection;
			uniform mat4 model;

out ivec2 position;

void main() {
	gl_Position = camProjection*camView*model*vec4(vertex_position, 1.0);
	position=ivec2((camProjection*camView*model*vec4(vertex_position, 1.0)).xy);
}

#pragma fragment
#define SNORM16_MAX_FLOAT_MINUS_EPSILON float(float(32768-2) / float(32768-1))
        #define FLOOD_ENCODE_OFFSET vec2(1.0f, SNORM16_MAX_FLOAT_MINUS_EPSILON)
        #define FLOOD_ENCODE_SCALE vec2(2.0f, 1.0f + SNORM16_MAX_FLOAT_MINUS_EPSILON)

        #define FLOOD_NULL_POS -1.0f
        #define FLOOD_NULL_POS_FLOAT2 vec2(FLOOD_NULL_POS, FLOOD_NULL_POS)

in ivec2 position;
out vec4 output_color;
uniform sampler2D renderTex;
uniform float stepWidth;

void main(void)
{
const float pos_infinity = 1f/0;
mat3 values;
ivec2 texelSize=textureSize(renderTex,0);
vec4 TexelSize=vec4(1.0f/texelSize,texelSize);
                  // initialize best distance at infinity
                float bestDist = pos_infinity;
                vec2 bestCoord;

                // jump samples
                for(int u=-1; u<=1; u++)
                {
                    for(int v=-1; v<=1; v++)
                    {
                        // calculate offset sample position
                        ivec2 offsetUV =ivec2( position + ivec2(u, v) * stepWidth);

                        // .Load() acts funny when sampling outside of bounds, so don't
                        offsetUV = clamp(offsetUV, ivec2(0,0), ivec2(TexelSize.zw) - 1);

                        // decode position from buffer
                        vec2 offsetPos = (texelFetch(renderTex,offsetUV, 0).rg + FLOOD_ENCODE_OFFSET) * TexelSize.zw / FLOOD_ENCODE_SCALE;

                        // the offset from current position
                        vec2 disp = position.xy - offsetPos;

                        // square distance
                        float dist = dot(disp, disp);

                        // if offset position isn't a null position or is closer than the best
                        // set as the new best and store the position
                        if (offsetPos.y != FLOOD_NULL_POS && dist < bestDist)
                        {
                            bestDist = dist;
                            bestCoord = offsetPos;
                        }
                    }
                }

                // if not valid best distance output null position, otherwise output encoded position
               
                output_color =vec4(isinf(bestDist) ? FLOOD_NULL_POS_FLOAT2 : bestCoord * TexelSize.xy * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET,0,1);
       //output_color =vec4(0,0,0,0);
};
