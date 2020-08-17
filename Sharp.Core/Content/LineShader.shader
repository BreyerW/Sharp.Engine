//https://developer.download.nvidia.com/SDK/9.5/Samples/DEMOS/OpenGL/src/cg_VolumeLine/docs/VolumeLine.pdf
#version 130

#pragma vertex
uniform mat4 model;
uniform mat4 camView;
uniform mat4 camProjection;
uniform float width;
uniform vec2 viewPort; //Width and Height of the viewport
uniform float len;
in vec3 vertex_position;
in vec2 vertex_texcoord;
//out vec2 lineCenter;
//out vec2 v_texCoord;

void main(void)
{
	//vec4 dir = camProjection * camView * model * line_dir;
	vec2 screenConvert = vec2(2.0 / viewPort.x, -2.0 / viewPort.y);
	vec4 start = camProjection * camView * model * vec4(vertex_position * len,1);
	vec4 end = camProjection * camView * model * vec4(mix(vec3(1,0,0),vec3(0,0,0),vertex_position.x)*len,1);
	
	vec3 middlepoint = normalize((start.xyz + end.xyz) / 2.0);
	vec3 lineoffset = end.xyz - start.xyz;
	vec3 linedir = normalize(lineoffset);
	float texcoef = abs(dot(linedir, middlepoint));
	//texcoef= max(((texcoef - 1) * (length(lineoffset) / (width/2.0))) + 1, 0);
	vec2 start2d = start.xy / start.w;
	vec2 end2d = end.xy / end.w;
	vec2 dir2d =normalize(start2d - end2d)*(width/2.0)*screenConvert.y*start.w*vertex_texcoord.y;
	start.xy = ( dir2d.yx) + start.xy;
	/*
	start.x = (start.x + dir2d.y);
	start.y = (start.y - dir2d.x/**screenConvert.y*start.w);*/
	gl_Position =start;
	/*vec4 pp = model * vec4(vertex_position.x,vertex_position.yz,1);
	vec2 vp = viewPort;
	lineCenter = 0.5 * (pp.xy + vec2(1, 1));
	v_texCoord = vertex_texcoord;
	gl_Position = pp;*/
};

#pragma fragment
//uniform vec2 viewPort;
uniform vec4 color;
//uniform float blend; //1.5..2.5
//in vec2 lineCenter;
//in vec2 v_texCoord;
out vec4 output_color;

void main(void)
{
	/*vec4 col = color;
	float d = length(lineCenter - v_texCoord.xy/viewPort);
	float w = width;
	if (d > w)
		col.w = 0;
	else
	col.w *=pow(float((w - d) / w), blend);*/
	output_color = color;
};
