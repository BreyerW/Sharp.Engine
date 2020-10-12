uniform vec4 colorId;
uniform float alphaThreshold;

vec4 EnablePicking(vec4 color){
	return step(alphaThreshold,color.a)*mix(color, colorId,colorId.a);
}
