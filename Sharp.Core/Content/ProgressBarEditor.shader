#version 130
//https://medium.com/@bgolus/progressing-in-circles-13452434fdb9
#pragma vertex
#define PI            3.14159265359f

in vec3 vertex_position;
in vec2 vertex_texcoord;
uniform mat4 camView;
uniform mat4 camProjection;
uniform mat4 model;
uniform float progress; //0..1f
uniform vec4 bar_color;
uniform vec4 background_color;
uniform float inner_radius;
uniform float outer_radius;
uniform vec4 outline_color;
uniform float outline_width;
uniform float angle;
uniform float end_angle;
uniform float flipU;
uniform float flipV;
out vec4 uvMask;
uniform float size;
void main(void)
{
	vec4 pos = camProjection*camView*model*vec4(vertex_position.xz*size, vertex_position.y * size, 1.0);;
    uvMask.xy =vertex_texcoord * 2.0 - 1.0;
	uvMask.x=mix(uvMask.x,-uvMask.x,flipU);
	uvMask.y=mix(-uvMask.y,uvMask.y,flipU);
	 float barFrac = progress;
                float sinX, cosX;
	// note: there are no protections for very narrow arcs!

                // rotate base masks by arc angle
               float minAngle = angle * (PI / 180.0) + (1.0 - end_angle) * PI;//TODO: move by half of end angle?
                sinX=sin(minAngle);
				cosX=cos(minAngle);
                mat2 minRotationMatrix = mat2(cosX, -sinX, sinX, cosX);
                uvMask.xy = (uvMask.xy*minRotationMatrix);

                // rotated mask for end of arc
                float maxAngle = end_angle * (PI * 2.0);
				sinX=sin(maxAngle);
				cosX=cos(maxAngle);
                mat2 maxRotationMatrix = mat2(cosX, -sinX, sinX, cosX);
                uvMask.w = -(uvMask.xy* maxRotationMatrix).x;

                // scale progress bar value based on arc range
                barFrac *= end_angle;
				  // angled mask for end of progress bar
                float angle = barFrac * (PI * 2.0) - PI;
                sinX=sin(angle);
				cosX=cos(angle);
               mat2 rotationMatrix = mat2(cosX, -sinX, sinX, cosX);
                uvMask.z = (uvMask.xy* rotationMatrix).x;

     gl_Position = pos;
}

#pragma fragment
uniform float flipU;
in vec4 uvMask;
uniform vec4 color;
uniform mat4 model;
uniform float progress; //0..1f
uniform vec4 bar_color;
uniform vec4 background_color;
uniform float inner_radius;
uniform float outer_radius;
uniform vec4 outline_color;
uniform float outline_width;
uniform float angle;
uniform float end_angle;
out vec4 frag_color;

float saturate(float x) {
				return clamp(x, 0.0, 1.0);
			}
			vec2 saturate(vec2 x) {
				return clamp(x,vec2(0.0),vec2(1.0));
			}

void main(void)
{
  float barFrac =progress;

                // radial gradient for circles
                float radialGrad = length(uvMask.xy);

                // accurate derivative length rather than fwidth
                float radialGradDeriv = length(vec2(dFdx(radialGrad), dFdy(radialGrad))) * 0.75f;

                // outer and inner circle masks for progress bar
               float outerEdge = outer_radius - radialGrad;
                float innerEdge = radialGrad - inner_radius;

                // progress bar circle edge mask
                float circleEdge = smoothstep(-radialGradDeriv, radialGradDeriv, min(outerEdge, innerEdge));

            //#if defined(USE_OUTLINE)
                // outline circle edge mask
              //  float outlineEdge = max(smoothstep(-radialGradDeriv, radialGradDeriv, min(outerEdge, innerEdge) + _OutlineWidth), circleEdge);
            //#endif // USE_OUTLINE

                // sharpen masks with screen space derivates
                float diag = uvMask.z / fwidth(uvMask.z);
                float vert = uvMask.x / fwidth(uvMask.x);

                // scale progress bar value based on arc range
               barFrac *= end_angle;

                // get arc end
                float arc_max_edge = uvMask.w / fwidth(uvMask.w) + 0.5;

                // init arc edges for outline
                float arc_outline_min = 0;
                float arc_outline_max = 0;

            //#if defined(USE_OUTLINE)
                // set offset arc edges for outline
            //    arc_outline_min = (i.uvMask.x - _OutlineWidth) / fwidth(i.uvMask.x);
            //    arc_outline_max = (i.uvMask.w - _OutlineWidth) / fwidth(i.uvMask.w);
            //#endif // USE_OUTLINE

                // arc masks for circle and outline edge mask
                float circleArcMask = 0f;
                float outlineArcMask = 0f;

                if ((end_angle) < 1.0f)
                {
                    // "flip" arc mask depending on if less than 180 degrees
                    //if ((end_angle) < 0.5f)
                    {
                        // arc is wedge
                        circleArcMask = max(vert, arc_max_edge);
                        outlineArcMask = max(arc_outline_min, arc_outline_max);
                    }
                    /*else
                    {
                        // remove wedge
                        circleArcMask = min(vert, arc_max_edge);
                        outlineArcMask = min(arc_outline_min, arc_outline_max);
                    }*/

                    // cut out arc wedge from circle edge mask
                    circleEdge = min(circleEdge, 1.0f - saturate(circleArcMask));

               // #if defined(USE_OUTLINE)
                    // cut out arc wedge from outline edge mask
                //    outlineEdge = min(outlineEdge, 1.0 - saturate(outlineArcMask));
                //#endif // USE_OUTLINE

                    // hack to prevent color bleed at the starting edge of the progress bar
                    vert -= 0.5f;
                }
				
                // "flip" the masks depending on progress bar value
                float barProgress = 0.0f;
                if (barFrac < 0.5f)
                    barProgress = max(diag, vert);
                else
                    barProgress = min(diag, vert);

                // mask bottom of progress bar when below 20%
				if(flipU<1f){
                if (abs(barFrac) < 0.2f && uvMask.y < 0.0f)
                    barProgress = 1.0f;
					}
					else{
					if (abs(barFrac) < 0.2f && uvMask.y > 0.0f)
                   barProgress = 1.0f;
					}
                barProgress = saturate(barProgress);

                // lerp between colors
                vec4 col = mix(bar_color, background_color, barProgress);

           // #if defined(USE_OUTLINE)
                // lerp to outline color if outline > 0.0
            //    if (outline_width > 0.0)
            //        col = mix(outline_color, col, circleEdge);

                // apply outline mask as alpha
             //   col.a *= outlineEdge;
            //#else // !defined(USE_OUTLINE)

                // apply circle mask as alpha
                col.a *= circleEdge;
            //#endif // USE_OUTLINE

	frag_color =col;
}