
//fog fragment shader
 
#version 330


layout(location = 0) out vec4 out_color;
 
uniform vec3 light_position;
uniform vec3 eye_position;
 
uniform sampler2D texture1;
 
//0 linear; 1 exponential; 2 exponential square
uniform int fogSelector;
//0 plane based; 1 range based
uniform int depthFog;
 
//can pass them as uniforms
const vec3 DiffuseLight = vec3(0.15, 0.05, 0.0);
const vec3 RimColor = vec3(0.2, 0.2, 0.2);
 
//from vertex shader
in vec3 world_pos;
in vec3 world_normal;
in vec4 viewSpace;
in vec2 texcoord;
 
const vec3 fogColor = vec3(0.5, 0.5,0.5);
 
void main(){
 
vec3 tex1 = texture(texture1, texcoord).rgb;
 
//get light an view directions
vec3 L = normalize( light_position - world_pos);
vec3 V = normalize( eye_position - world_pos);
 
//diffuse lighting
vec3 diffuse = DiffuseLight * max(0, dot(L,world_normal));
 
//rim lighting
float rim = 1 - max(dot(V, world_normal), 0.0);
rim = smoothstep(0.6, 1.0, rim);
vec3 finalRim = RimColor * vec3(rim, rim, rim);
//get all lights and texture
vec3 lightColor = finalRim + diffuse + tex1;
 
vec3 finalColor = vec3(0, 0, 0);
 
//compute range based distance
float dist = length(viewSpace);
 
//my camera y is 10.0. you can change it or pass it as a uniform
float be = (10.0 - viewSpace.y) * 0.004;//0.004 is just a factor; change it if you want
float bi = (10.0 - viewSpace.y) * 0.001;//0.001 is just a factor; change it if you want
 
//OpenGL SuperBible 6th edition uses a smoothstep function to get
//a nice cutoff here
//You have to tweak this values
// float be = 0.025 * smoothstep(0.0, 6.0, 32.0 - viewSpace.y);
// float bi = 0.075* smoothstep(0.0, 80, 10.0 - viewSpace.y);
 
float ext = exp(-dist * be);
float insc = exp(-dist * bi);
 
finalColor = lightColor * ext + fogColor * (1 - insc);
 
out_color = vec4(finalColor, 1);
 
}