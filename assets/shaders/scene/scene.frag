#version 330 core
in vec3 vWorldNormal;
in vec3 vColor;
in vec3 vWorldPosition;
out vec4 FragColor;

const int MAX_POINT_LIGHTS = 16;

uniform vec3 uLightDirection;
uniform vec3 uLightColor;
uniform float uAmbientStrength;
uniform float uDiffuseStrength;
uniform vec3 uFogColor;
uniform float uFogNear;
uniform float uFogFar;
uniform vec3 uCameraPosition;
uniform int uPointLightCount;
uniform vec3 uPointLightPosition[MAX_POINT_LIGHTS];
uniform vec3 uPointLightColor[MAX_POINT_LIGHTS];
uniform float uPointLightRadius[MAX_POINT_LIGHTS];
uniform float uPointLightIntensity[MAX_POINT_LIGHTS];

void main()
{
    vec3 normal = normalize(vWorldNormal);
    vec3 lightDir = normalize(uLightDirection);
    float diffuse = max(dot(normal, lightDir), 0.0);
    float hemi = 0.5 + 0.5 * clamp(normal.y, 0.0, 1.0);

    vec3 pointLighting = vec3(0.0);
    for (int i = 0; i < uPointLightCount; i++)
    {
        vec3 toLight = uPointLightPosition[i] - vWorldPosition;
        float distanceToLight = length(toLight);
        if (distanceToLight >= uPointLightRadius[i])
        {
            continue;
        }

        vec3 pointDirection = distanceToLight > 0.0001 ? toLight / distanceToLight : vec3(0.0, 1.0, 0.0);
        float pointDiffuse = max(dot(normal, pointDirection), 0.0);
        float attenuation = 1.0 - clamp(distanceToLight / max(uPointLightRadius[i], 0.001), 0.0, 1.0);
        attenuation *= attenuation;
        float pointContribution = (0.18 + pointDiffuse) * attenuation * uPointLightIntensity[i];
        pointLighting += uPointLightColor[i] * pointContribution;
    }

    float baseLight = uAmbientStrength + diffuse * uDiffuseStrength + hemi * 0.18;
    vec3 litColor = vColor * (baseLight * uLightColor + pointLighting);

    float fogDistance = distance(vWorldPosition, uCameraPosition);
    float fogFactor = clamp((fogDistance - uFogNear) / max(uFogFar - uFogNear, 0.001), 0.0, 1.0);
    vec3 finalColor = mix(litColor, uFogColor, fogFactor);

    FragColor = vec4(finalColor, 1.0);
}
