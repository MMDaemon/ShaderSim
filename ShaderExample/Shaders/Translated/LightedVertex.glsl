#version 430 core

uniform mat4 Camera;
in mat4 InstanceTransformation;
in vec3 Pos;
in vec3 Normal;
in vec4 Color;
out vec4 Col;
out vec3 WorldPos;
out vec3 N;

void main()
{
        WorldPos = (InstanceTransformation * vec4(Pos, 1)).xyz;
        N = (InstanceTransformation * vec4(Normal, 0)).xyz;
        gl_Position = Camera * InstanceTransformation * vec4(Pos, 1);
        Col = Color;
}