#version 330 core
in vec3 vColor;
in float vAlpha;
in float vAcross;
out vec4 FragColor;

void main()
{
    float edge = 1.0 - abs(vAcross);
    if (edge <= 0.0) discard;

    float core = pow(edge, 3.0);
    float glow = pow(edge, 1.2);
    float intensity = core * 0.85 + glow * 0.55;
    float alpha = vAlpha * intensity;
    if (alpha < 0.003) discard;

    vec3 hot = mix(vColor, vec3(1.0, 1.0, 1.0), core * 0.65);
    FragColor = vec4(hot, alpha);
}
