﻿struct Light {
   vec3 position;
   vec4 color;
   float intensity;
};
uniform float ambient;

#ifdef PHONG_LIGHT
vec4 ApplyLight(mat4 model,mat4 cam,Light light,vec4 surfaceColor,vec3 fragNormal,vec3 fragVert,float Shininess,vec3 SpecularColor) {

vec3 normal = normalize(transpose(inverse(mat3(model))) * fragNormal);
    vec3 surfacePos = vec3(model * vec4(fragVert, 1));
    vec3 surfaceToLight = normalize(light.position - surfacePos);
    vec3 surfaceToCamera = normalize(cam[3].xyz - surfacePos);
    
    //ambient
    vec3 ambientColor = ambient * surfaceColor.rgb * light.color;

    //diffuse
    float diffuseCoefficient = max(0.0, dot(normal, surfaceToLight));
    vec3 diffuse = diffuseCoefficient * surfaceColor.rgb * light.color;
    
    //specular
    float specularCoefficient = 0.0;
    if(diffuseCoefficient > 0.0)
        specularCoefficient = pow(max(0.0, dot(surfaceToCamera, reflect(-surfaceToLight, normal))),Shininess);
    vec3 specular = specularCoefficient * SpecularColor * light.color;
    
    //attenuation
    float distanceToLight = length(light.position - surfacePos);
    float attenuation = 1.0 / (1.0 + (1-light.intensity) * pow(distanceToLight, 2));

    //linear color (color before gamma correction)
    vec3 linearColor =ambientColor + attenuation*(diffuse + specular);
    
    //final color (after gamma correction)
    vec3 gamma = vec3(1.0/2.2);

   return vec4(pow(linearColor, gamma), surfaceColor.a);

}
#endif
