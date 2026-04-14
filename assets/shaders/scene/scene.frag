#version 330 core
in vec3 vWorldNormal;
in vec3 vColor;
in vec3 vWorldPosition;
out vec4 FragColor;

void main()
{
    vec3 normal = normalize(vWorldNormal);
    vec3 lightDir = normalize(vec3(-0.45, 1.0, -0.30));
    float diffuse = max(dot(normal, lightDir), 0.0);
    float hemi = 0.5 + 0.5 * clamp(normal.y, 0.0, 1.0);
    float depthTint = clamp(1.0 - abs(vWorldPosition.z) * 0.015, 0.82, 1.0);
    float light = 0.22 + diffuse * 0.50 + hemi * 0.28;
    FragColor = vec4(vColor * light * depthTint, 1.0);
}
