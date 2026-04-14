#version 330 core
in vec3 vWorldNormal;
in vec3 vColor;
out vec4 FragColor;

void main()
{
    vec3 lightDir = normalize(vec3(0.55, 1.0, 0.35));
    float diffuse = max(dot(normalize(vWorldNormal), lightDir), 0.0);
    float light = 0.45 + diffuse * 0.55;
    FragColor = vec4(vColor * light, 1.0);
}
