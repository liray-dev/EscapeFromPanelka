#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aColor;

uniform mat4 uModel;
uniform mat4 uMvp;
uniform vec4 uTint;

out vec3 vWorldNormal;
out vec3 vColor;
out vec3 vWorldPosition;

void main()
{
    vec4 worldPosition = vec4(aPosition, 1.0) * uModel;
    vWorldPosition = worldPosition.xyz;
    vWorldNormal = normalize(aNormal * mat3(uModel));
    vColor = aColor * uTint.rgb;
    gl_Position = vec4(aPosition, 1.0) * uMvp;
}
