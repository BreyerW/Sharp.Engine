struct Light {
   vec3 position;
   vec4 color;
};


#ifdef PHONG_LIGHT
vec4 ApplyLight(mat4 model,Light light,vec3 fragNormal,vec3 fragVert) {

    //calculate normal in world coordinates
    mat3 normalMatrix = transpose(inverse(mat3(model)));
    vec3 normal = normalize(normalMatrix * fragNormal);
    
    //calculate the location of this fragment (pixel) in world coordinates
    vec3 fragPosition = vec3(model * vec4(fragVert, 1));
    
    //calculate the vector from this pixels surface to the light source
    vec3 surfaceToLight = light.position - fragPosition;

    //calculate the cosine of the angle of incidence
    float brightness = dot(normal, surfaceToLight) / (length(surfaceToLight) * length(normal));
    brightness = clamp(brightness, 0, 1);

    //calculate final color of the pixel, based on:
    // 1. The angle of incidence: brightness
    // 2. The color/intensities of the light: lights[0].intensities
    // 3. The texture and texture coord: texture(tex, fragTexCoord)
    return brightness * light.color;

}
#endif
