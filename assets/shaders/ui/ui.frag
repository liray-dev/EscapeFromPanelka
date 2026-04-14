#version 330 core
in vec2 vTexCoord;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uTexture0;

void main()
{
    vec4 tex = texture(uTexture0, vTexCoord);
    FragColor = tex * vColor;
}
