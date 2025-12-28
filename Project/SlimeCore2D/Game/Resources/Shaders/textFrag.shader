#version 300 es
precision mediump float;
in vec2 TexCoords;
out vec4 color;

uniform sampler2D text;
uniform vec3 textColor;

void main()
{
    vec4 sampled = vec4(1.0, 1.0, 1.0, texture(text, TexCoords).r);
    vec4 result = vec4(textColor, 1.0) * sampled;
    // discard fragments with (almost) zero alpha to avoid rectangular artifacts
    if (result.a < 0.01)
        discard;
    color = result;
}