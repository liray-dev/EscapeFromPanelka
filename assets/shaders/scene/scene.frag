#version 330 core
in vec3 vWorldNormal;
in vec3 vColor;
in vec3 vWorldPosition;
out vec4 FragColor;

uniform vec3 uLightDirection;
uniform vec3 uLightColor;
uniform float uAmbientStrength;
uniform float uDiffuseStrength;
uniform vec3 uFogColor;
uniform float uFogNear;
uniform float uFogFar;
uniform vec3 uCameraPosition;

void main()
{
    vec3 normal = normalize(vWorldNormal);
    vec3 lightDir = normalize(uLightDirection);
    float diffuse = max(dot(normal, lightDir), 0.0);
    float hemi = 0.5 + 0.5 * clamp(normal.y, 0.0, 1.0);
    float baseLight = uAmbientStrength + diffuse * uDiffuseStrength + hemi * 0.18;
    vec3 litColor = vColor * baseLight * uLightColor;

    float fogDistance = distance(vWorldPosition, uCameraPosition);
    float fogFactor = clamp((fogDistance - uFogNear) / max(uFogFar - uFogNear, 0.001), 0.0, 1.0);
    vec3 finalColor = mix(litColor, uFogColor, fogFactor);

    FragColor = vec4(finalColor, 1.0);
}
