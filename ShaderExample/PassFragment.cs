﻿using ShaderSim;
using ShaderSim.Attributes;
using ShaderSim.Mathematics;

namespace ShaderExample
{
    class PassFragment : FragmentShader
    {
        [In]
        public Vector4 Col { private get; set; }

        public override void Main()
        {
            Color = Col;
        }
    }
}
