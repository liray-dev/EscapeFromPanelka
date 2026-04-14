#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vWorldNormal;
out vec3 vColor;

void main()
{
    vec4 worldPosition = vec4(aPosition, 1.0) * uModel;
    mat3 normalMatrix = transpose(inverse(mat3(uModel)));
    vWorldNormal = normalize(aNormal * normalMatrix);
    vColor = aColor;
    gl_Position = worldPosition * uView * uProjection;
}
