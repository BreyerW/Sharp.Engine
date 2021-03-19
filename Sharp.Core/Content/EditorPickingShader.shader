uniform vec4 colorId;
uniform float enablePicking;
uniform float alphaThreshold;
uniform vec2 mousePos;
vec4 AlphaAwarePicking(vec4 color){
	float condition = step(alphaThreshold, color.a);
	gl_FragDepth = mix(gl_FragCoord.z,mix(0.0f, gl_FragCoord.z, condition), enablePicking);
	return mix(color, mix(vec4(0.0f), colorId, condition), enablePicking);
}
