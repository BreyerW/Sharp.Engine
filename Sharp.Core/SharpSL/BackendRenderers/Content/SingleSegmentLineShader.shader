
#version 140

#pragma vertex

in vec3 vertex_position;
in vec3 prev_position;
in vec3 next_position;
in float dir;
uniform mat4 camView;
uniform mat4 camWorldView;
uniform mat4 camProjection;
uniform mat4 model;
uniform float width;
uniform float len;
uniform vec2 viewPort;
uniform float miter;

vec3 getCameraPos() {
	return vec3(camWorldView[3][0], camWorldView[3][1], camWorldView[3][2]);
}
vec3 getObjectPos() {
	mat4 m = camProjection * camView * model;
	return vec3(m[3][0], m[3][1], m[3][2]);
}
vec3 getCameraDir() {
	mat4 m =  camView;
	return vec3(m[2][0], m[2][1], m[2][2]);
}
void main(void) {

	/*vec2 aspectVec = vec2(viewPort.x/viewPort.y, 1.0);
	mat4 projViewModel = camProjection * camView * model;
	vec4 previousProjected = projViewModel * vec4(prev_position*len, 1.0);
	vec4 currentProjected = projViewModel * vec4(vertex_position*len, 1.0);
	vec4 nextProjected = projViewModel * vec4(next_position*len, 1.0);

	

	//get 2D screen space with W divide and aspect correction
	vec2 currentScreen = currentProjected.xy / currentProjected.w * aspectVec;
	vec2 previousScreen = previousProjected.xy / previousProjected.w * aspectVec;
	vec2 nextScreen = nextProjected.xy / nextProjected.w * aspectVec;
	float orientation = dir;

	//starting point uses (next - current)
	vec2 direction = vec2(0.0);
	if (currentScreen == previousScreen) {
		direction = normalize(nextScreen - currentScreen);
	}
	//ending point uses (current - previous)
	else if (currentScreen == nextScreen) {
		direction = normalize(currentScreen - previousScreen);
	}
	//somewhere in middle, needs a join
	else {
		//get directions from (C - B) and (B - A)
		vec2 dirA = normalize((currentScreen - previousScreen));
		/*if (miter == 1) {
			vec2 dirB = normalize((nextScreen - currentScreen));
			//now compute the miter join normal and length
			vec2 tangent = normalize(dirA + dirB);
			vec2 perp = vec2(-dirA.y, dirA.x);
			vec2 miter = vec2(-tangent.y, tangent.x);
			dir = tangent;
			len = width / dot(miter, perp);
		}
		else {*
			direction = dirA;
		//}
	}
	vec2 normal = vec2(-direction.y,direction.x);
	normal *= (width/2.0f) * currentProjected.w/250f;
	normal.x /= aspectVec.x;

	vec4 offset = vec4(normal * orientation, 0.0, 0.0);
	gl_Position = currentProjected + offset; */
	
	/*mat4 s = model*0;
	s[0][0] = scale;
	s[1][1] = scale;
	s[2][2] = scale;
	s[3][3] = 1;*/
	float isLastPoint =sign(length(vertex_position-next_position));
	vec3 next_v = mix(prev_position, next_position,isLastPoint);
	mat4 m=camProjection * camView * model;
	vec4 start =  m*vec4(vertex_position*len, 1);
	vec4 end =m * vec4(next_v *len, 1);

	vec2 start2d =start.xy/start.w;
	vec2 end2d = end.xy/end.w;

	vec2 dir2d = normalize(mix(start2d - end2d, end2d - start2d,isLastPoint));

	vec3 middlepoint = normalize((start.xyz + end.xyz) / 2.0);
	vec3 lineoffset = end.xyz - start.xyz;
	vec3 linedir = normalize(lineoffset);
	float texcoef = abs(dot(linedir, middlepoint));
	texcoef = max(((texcoef - 1) * ((len / viewPort.x) / (width / viewPort.y))) + 1, 0);
	start.xy = ((texcoef * (width / viewPort) *-dir) * dir2d.xy) + start.xy;

	dir2d = dir * dir2d * (width/viewPort.yx)* start.w;

	start.x -= dir2d.y; // vertical x
	start.y += dir2d.x; // vertical y
	
	gl_Position =vec4(start.xyzw);
}

#pragma fragment

//#ifdef GL_ES
//precision mediump float;
//#endif

out vec4 frag_color;
uniform vec4 color;

void main() {

	frag_color = color;

}
vec4 when_eq(vec4 x, vec4 y) {
	return 1.0 - abs(sign(x - y));
}

vec4 when_neq(vec4 x, vec4 y) {
	return abs(sign(x - y));
}

vec4 when_gt(vec4 x, vec4 y) {
	return max(sign(x - y), 0.0);
}

vec4 when_lt(vec4 x, vec4 y) {
	return max(sign(y - x), 0.0);
}

vec4 when_ge(vec4 x, vec4 y) {
	return 1.0 - when_lt(x, y);
}

vec4 when_le(vec4 x, vec4 y) {
	return 1.0 - when_gt(x, y);
}
vec4 and(vec4 a, vec4 b) {
	return a * b;
}

vec4 or(vec4 a, vec4 b) {
	return min(a + b, 1.0);
}

/*vec4 xor(vec4 a, vec4 b) {
	return (a + b) % 2;
}*/

vec4 not(vec4 a) {
	return 1.0 - a;
}