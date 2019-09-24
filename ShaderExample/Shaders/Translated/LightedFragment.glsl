#version 430 core
out vec4 Color;
uniform vec3 CameraPosition;
uniform vec4 AmbientLightColor;
uniform vec4 LightColor;
uniform vec3 LightDirection;
in vec4 Col;
in vec3 WorldPos;
in vec3 N;

float Lambert(vec3 n, vec3 l)
{
        return max(0, dot(n, l));
}

float Specular(vec3 n, vec3 l, vec3 v, float  shininess)
{
        vec3 r = reflect(-l, n);
        float  illuminated = step(dot(n, l), 0);
        return pow(max(0, dot(r, v)), shininess) * illuminated;
}

void main()
{
        vec3 normal = normalize(N);
        vec4 ambient = AmbientLightColor * Col;
        vec4 diffuse = Col * LightColor * Lambert(normal, -LightDirection);
        vec4 specular = LightColor * Specular(normal, LightDirection, normalize(CameraPosition - WorldPos), 99.9);
        Color = ambient + diffuse + specular;
}