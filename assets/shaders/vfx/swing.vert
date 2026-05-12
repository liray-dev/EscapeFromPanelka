#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
layout(location = 2) in float aAlpha;
layout(location = 3) in float aAcross;

uniform mat4 uMvp;

out vec3 vColor;
out float vAlpha;
out float vAcross;

void main()
{
    vColor = aColor;
    vAlpha = aAlpha;
    vAcross = aAcross;
    gl_Position = vec4(aPosition, 1.0) * uMvp;
}
