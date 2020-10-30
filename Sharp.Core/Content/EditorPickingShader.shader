uniform vec4 colorId;
uniform float enablePicking;
uniform float alphaThreshold;
uniform vec2 mousePos;//TODO: enable object highlight when mouse moves without picking
vec4 EnablePicking(vec4 color){

	if(enablePicking == 1f){
		if(step(alphaThreshold,color.a)==0)
		discard;
		return colorId;
		}
		return color;
}
