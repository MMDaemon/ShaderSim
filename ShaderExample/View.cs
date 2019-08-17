﻿using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using ShaderExample.Shaders;
using ShaderExample.Utils;
using ShaderRenderer;
using ShaderUtils;
using ShaderSimulator;
using ShaderTranslator;
using Matrix4x4 = ShaderUtils.Mathematics.Matrix4x4;
using Vector4 = ShaderUtils.Mathematics.Vector4;

namespace ShaderExample
{
    class View
    {
        private readonly RenderWrapper _wrapper;
        private VertexShader _vertex;
        private FragmentShader _fragment;

        readonly CameraPerspective _camera = new CameraPerspective();

        public View(bool debug = false)
        {
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);

            _wrapper = debug ? (RenderWrapper)new RenderSimulator() : new RenderTranslator();

            _vertex = new LightedVertex();
            _fragment = new PassFragment();
            if (_wrapper is RenderTranslator translator)
            {
                string directory = Directory.GetCurrentDirectory();
                translator.RegisterShader(_vertex, _fragment, directory + @"\..\..\Shaders\" + _vertex.GetType().Name + ".cs", directory + @"\..\..\Shaders\" + _fragment.GetType().Name + ".cs");
            }
        }

        public void Render(IEnumerable<Entity> entities)
        {
            Dictionary<IVertexArrayObject, int> vaoInformations = PrepareVAOs(entities);
            _camera.Position = new System.Numerics.Vector3(0f, 0f, 1f);
            _wrapper.ActivateShader(_vertex, _fragment);
            //_wrapper.SetUniform("Camera", (Matrix4x4)_camera.CalcMatrix());
            _wrapper.SetUniform("Camera", (Matrix4x4)System.Numerics.Matrix4x4.Transpose(System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0f, -0.5f, 0))));

            List<Texture2D> layers = new List<Texture2D>();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (var vaoInformation in vaoInformations)
            {
                vaoInformation.Key.Draw(vaoInformation.Value);
                if (_wrapper is RenderSimulator)
                {
                    layers.Add(new Texture2D(((RenderSimulator)_wrapper).RenderResult));
                }
            }
            _wrapper.DeactivateShader();

            if (_wrapper is RenderSimulator)
            {
                GL.Enable(EnableCap.Texture2D);
                layers[0].Bind();

                Drawer.DrawScreenQuad();

                Texture2D.Unbind();
                GL.Disable(EnableCap.Texture2D);
            }
        }

        private Dictionary<IVertexArrayObject, int> PrepareVAOs(IEnumerable<Entity> entities)
        {
            Dictionary<IVertexArrayObject, int> simulatorVAOs = new Dictionary<IVertexArrayObject, int>();

            Dictionary<Enums.EntityType, IVertexArrayObject> simVAOs = new Dictionary<Enums.EntityType, IVertexArrayObject>();
            Dictionary<Enums.EntityType, List<Matrix4x4>> transformations = new Dictionary<Enums.EntityType, List<Matrix4x4>>();
            Dictionary<Enums.EntityType, List<Vector4>> colors = new Dictionary<Enums.EntityType, List<Vector4>>();
            Dictionary<Enums.EntityType, int> instanceCounts = new Dictionary<Enums.EntityType, int>();

            foreach (var entity in entities)
            {
                if (!simVAOs.ContainsKey(entity.Type))
                {
                    Mesh mesh = null;
                    switch (entity.Type)
                    {
                        case Enums.EntityType.Triangle:
                            mesh = MeshCreator.CreateTriangle();
                            break;
                    }

                    if (mesh != null)
                    {
                        switch (_wrapper)
                        {
                            case RenderSimulator _:
                                simVAOs.Add(entity.Type, VAOLoader.FromMesh<SimulatorVAO>(mesh, new Tuple<VertexShader, FragmentShader>(_vertex, _fragment), new object[] { _wrapper }));
                                break;
                            case RenderTranslator _:
                                simVAOs.Add(entity.Type, VAOLoader.FromMesh<TranslatorVAO>(mesh, new Tuple<VertexShader, FragmentShader>(_vertex, _fragment), new object[] { _wrapper }));
                                break;
                        }
                        transformations.Add(entity.Type, new List<Matrix4x4>());
                        colors.Add(entity.Type, new List<Vector4>());
                        instanceCounts.Add(entity.Type, 0);
                    }
                }
                if (transformations.ContainsKey(entity.Type))
                {
                    transformations[entity.Type].Add(entity.Transformation);
                    colors[entity.Type].Add(entity.Color);
                    instanceCounts[entity.Type]++;
                }
            }

            foreach (var key in simVAOs.Keys)
            {
                simVAOs[key].SetAttribute("InstanceTransformation", new Tuple<VertexShader, FragmentShader>(_vertex, _fragment), transformations[key], true);
                simVAOs[key].SetAttribute("Color", new Tuple<VertexShader, FragmentShader>(_vertex, _fragment), colors[key], true);
                simulatorVAOs.Add(simVAOs[key], instanceCounts[key]);
            }

            return simulatorVAOs;
        }

        public void Resize(int width, int height)
        {
            GL.Viewport(0, 0, width, height);
            _wrapper.SetRenderSize(width, height);
            _camera.Aspect = (float)width / height;
        }
    }
}
