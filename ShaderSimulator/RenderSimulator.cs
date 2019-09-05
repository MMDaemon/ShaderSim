using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ShaderUtils;
using ShaderUtils.Mathematics;

namespace ShaderSimulator
{
    public class RenderSimulator : RenderWrapper
    {
        public override bool DepthEnabled { get; set; }

        private readonly Dictionary<string, object> _uniforms;

        protected readonly Dictionary<IVertexArrayObject, IList<uint>> RenderData;
        protected readonly Dictionary<Shader, Dictionary<string, IList>> Attributes;
        protected readonly Dictionary<Shader, Dictionary<string, IList>> InstancedAttributes;

        private VertexShader _activeVertexShader;
        private FragmentShader _activeFragmentShader;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _interim = 0;

        public Bitmap RenderResult { get; private set; }

        private readonly MethodInfo _setValueMethod;

        private readonly List<Vector4> _vertexPositions;
        private readonly Dictionary<string, IList> _vertexValues;
        private readonly List<Triangle> _primitives;
        private List<float>[,] _depths;
        private Dictionary<string, IList>[,] _fragments;

        public RenderSimulator()
        {
            DepthEnabled = true;
            _uniforms = new Dictionary<string, object>();
            RenderData = new Dictionary<IVertexArrayObject, IList<uint>>();
            Attributes = new Dictionary<Shader, Dictionary<string, IList>>();
            InstancedAttributes = new Dictionary<Shader, Dictionary<string, IList>>();

            _setValueMethod = typeof(Shader).GetMethod("SetValue");

            _vertexPositions = new List<Vector4>();
            _vertexValues = new Dictionary<string, IList>();
            _primitives = new List<Triangle>();
        }

        public override void ActivateShader(VertexShader vertex, FragmentShader fragment)
        {
            _activeVertexShader = vertex;
            _activeFragmentShader = fragment;
        }

        public override void DeactivateShader()
        {
            _activeVertexShader = null;
            _activeFragmentShader = null;
        }

        public override void SetUniform<T>(string name, T value)
        {
            if (_uniforms.ContainsKey(name))
            {
                _uniforms[name] = value;
            }
            else
            {
                _uniforms.Add(name, value);
            }
        }

        public void SetRenderData(SimulatorVAO vao, IEnumerable<uint> data)
        {
            if (RenderData.ContainsKey(vao))
            {
                RenderData[vao] = (IList<uint>)data;
            }
            else
            {
                RenderData.Add(vao, (IList<uint>)data);
            }
        }

        public void SetAttributes<T>(Shader shader, string name, IList<T> values, bool perInstance) where T : struct
        {
            if (typeof(T) == typeof(Matrix4x4))
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    values[i] = (T)(object)(Matrix4x4)System.Numerics.Matrix4x4.Transpose((Matrix4x4)(object)values[i]);
                }
            }
            if (perInstance)
            {
                if (InstancedAttributes.ContainsKey(shader))
                {
                    if (InstancedAttributes[shader].ContainsKey(name))
                    {
                        InstancedAttributes[shader][name] = (IList)values;
                    }
                    else
                    {
                        InstancedAttributes[shader].Add(name, (IList)values);
                    }
                }
                else
                {
                    InstancedAttributes.Add(shader, new Dictionary<string, IList>());
                    InstancedAttributes[shader].Add(name, (IList)values);
                }
            }
            else
            {
                if (Attributes.ContainsKey(shader))
                {
                    if (Attributes[shader].ContainsKey(name))
                    {
                        Attributes[shader][name] = (IList)values;
                    }
                    else
                    {
                        Attributes[shader].Add(name, (IList)values);
                    }
                }
                else
                {
                    Attributes.Add(shader, new Dictionary<string, IList>());
                    Attributes[shader].Add(name, (IList)values);
                }
            }
        }

        public void DrawElementsInstanced(int instanceCount = 1)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            double time = 0;
            _interim = 0;

            Console.WriteLine("Setting uniforms");
            SetUniforms();
            time = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Uniforms set. Duration: {time - _interim}ms");
            _interim = time;
            Console.WriteLine("Calculating vertex step");
            CalculateVertexStep(instanceCount);
            time = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Vertex step calculated. Duration: {time - _interim}ms");
            _interim = time;
            Console.WriteLine("Assembling primitives");
            GeneratePrimitives();
            time = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Primitives assembled. Duration: {time - _interim}ms");
            _interim = time;
            Console.WriteLine("Rasterization");
            CalculateFragments();
            time = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Rasterization complete. Duration: {time - _interim}ms");
            _interim = time;
            Console.WriteLine("Calculating fragment step");
            RenderResult = CalculateFragmentStep();
            time = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Fragment step calculated. Duration: {time - _interim}ms");
            _interim = time;

            Console.WriteLine("Cleanup");

            Attributes.Clear();
            InstancedAttributes.Clear();
            RenderData.Clear();

            _vertexPositions.Clear();
            _vertexValues.Clear();
            _primitives.Clear();

            time = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Cleanup complete. Duration: {time - _interim}ms");
            _interim = time;
            _stopwatch.Stop();
        }

        private void SetUniforms()
        {
            if (_setValueMethod != null)
            {
                foreach (var uniform in _uniforms)
                {
                    SetUniform(uniform.Key, uniform.Value);
                }
            }
        }

        private void SetUniform(string name, object value)
        {
            if (_activeVertexShader.UniformProperties.ContainsKey(name))
            {
                _activeVertexShader.UniformProperties[name].SetValue(_activeVertexShader, value);
            }

            if (_activeFragmentShader.UniformProperties.ContainsKey(name))
            {
                _activeFragmentShader.UniformProperties[name].SetValue(_activeFragmentShader, value);
            }
        }

        private void CalculateVertexStep(int instanceCount)
        {
            for (int i = 0; i < instanceCount; i++)
            {
                foreach (var instancedAttribute in InstancedAttributes[_activeVertexShader])
                {
                    SetAttribute(_activeVertexShader, instancedAttribute.Key, instancedAttribute.Value[i]);
                }

                for (int j = 0; j < RenderData[ActiveVAO].Count; j++)
                {
                    foreach (var attribute in Attributes[_activeVertexShader])
                    {
                        SetAttribute(_activeVertexShader, attribute.Key, attribute.Value[(int)RenderData[ActiveVAO][j]]);
                    }

                    _activeVertexShader.Main();

                    _vertexPositions.Add(_activeVertexShader.Position * (1 / _activeVertexShader.Position.W));

                    foreach (var outValue in _activeVertexShader.GetOutValues())
                    {
                        if (_vertexValues.ContainsKey(outValue.Key))
                        {
                            _vertexValues[outValue.Key].Add(outValue.Value);
                        }
                        else
                        {
                            _vertexValues.Add(outValue.Key, new List<object> { outValue.Value });
                        }
                    }
                }
            }
        }

        private void SetAttribute(Shader shader, string name, object value)
        {
            if (shader.InProperties.ContainsKey(name))
            {
                shader.InProperties[name].SetValue(shader, value);
            }
        }

        private void GeneratePrimitives()
        {
            for (int i = 0; i < _vertexValues.First().Value.Count; i += 3)
            {
                Triangle triangle = new Triangle();
                for (int j = 0; j < 3; j++)
                {
                    triangle[j].Add(VertexShader.PositionName, _vertexPositions[i + j]);
                    foreach (var key in _vertexValues.Keys)
                    {
                        triangle[j].Add(key, _vertexValues[key][i + j]);
                    }
                }
                _primitives.Add(triangle);
            }
        }

        private void CalculateFragments()
        {
            Vector2[,] positions = new Vector2[Width, Height];

            _depths = new List<float>[Width, Height];
            _fragments = new Dictionary<string, IList>[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    positions[x, y] = CalculatePosition(x, y);
                    foreach (var primitive in _primitives)
                    {
                        Vector3 baricentric = Barycentric(positions[x, y], primitive);
                        if (baricentric.X >= 0 && baricentric.Y >= 0 && baricentric.Z >= 0)
                        {
                            if (_depths[x, y] == null)
                            {
                                _depths[x, y] = new List<float>();
                            }

                            _depths[x, y].Add(((Vector4)primitive[0][VertexShader.PositionName]).Z * baricentric.X +
                                            ((Vector4)primitive[1][VertexShader.PositionName]).Z * baricentric.Y +
                                            ((Vector4)primitive[2][VertexShader.PositionName]).Z * baricentric.Z);

                            if (_fragments[x, y] == null)
                            {
                                _fragments[x, y] = new Dictionary<string, IList>();
                            }
                            foreach (var key in _vertexValues.Keys)
                            {
                                if (!_fragments[x, y].ContainsKey(key))
                                {
                                    _fragments[x, y].Add(key, new List<object>());
                                }

                                switch (primitive[0][key])
                                {
                                    case float _:
                                        {
                                            var p0 = (float)primitive[0][key] * baricentric.X;
                                            var p1 = (float)primitive[1][key] * baricentric.Y;
                                            var p2 = (float)primitive[2][key] * baricentric.Z;
                                            _fragments[x, y][key].Add(p0 + p1 + p2);
                                            break;
                                        }
                                    case Vector2 _:
                                        {
                                            var p0 = (Vector2)primitive[0][key] * baricentric.X;
                                            var p1 = (Vector2)primitive[1][key] * baricentric.Y;
                                            var p2 = (Vector2)primitive[2][key] * baricentric.Z;
                                            _fragments[x, y][key].Add(p0 + p1 + p2);
                                            break;
                                        }
                                    case Vector3 _:
                                        {
                                            var p0 = (Vector3)primitive[0][key] * baricentric.X;
                                            var p1 = (Vector3)primitive[1][key] * baricentric.Y;
                                            var p2 = (Vector3)primitive[2][key] * baricentric.Z;
                                            _fragments[x, y][key].Add(p0 + p1 + p2);
                                            break;
                                        }
                                    case Vector4 _:
                                        {
                                            var p0 = (Vector4)primitive[0][key] * baricentric.X;
                                            var p1 = (Vector4)primitive[1][key] * baricentric.Y;
                                            var p2 = (Vector4)primitive[2][key] * baricentric.Z;
                                            _fragments[x, y][key].Add(p0 + p1 + p2);
                                            break;
                                        }
                                    default:
                                        {
                                            MethodInfo mult = primitive[0][key].GetType().GetMethod("op_Multiply", new Type[] { primitive[0][key].GetType(), typeof(float) });
                                            MethodInfo add = primitive[0][key].GetType().GetMethod("op_Addition", new Type[] { primitive[0][key].GetType(), primitive[0][key].GetType() });
                                            var value0 = mult.Invoke(primitive[0][key], new object[] { primitive[0][key], baricentric.X });
                                            var value1 = mult.Invoke(primitive[1][key], new object[] { primitive[1][key], baricentric.Y });
                                            var value2 = mult.Invoke(primitive[2][key], new object[] { primitive[2][key], baricentric.Z });
                                            var add1 = add.Invoke(value0, new object[] { value0, value1 });
                                            var add2 = add.Invoke(add1, new object[] { add1, value2 });
                                            _fragments[x, y][key].Add(add2);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }
        }

        private Vector2 CalculatePosition(int x, int y)
        {
            Vector2 fragSize = new Vector2(2f / Width, 2f / Height);

            return new Vector2(fragSize.X * x + fragSize.X / 2 - 1, fragSize.Y * y + fragSize.Y / 2 - 1);
        }

        private Vector3 Barycentric(System.Numerics.Vector2 pos, Triangle triangle)
        {
            System.Numerics.Vector2 t0 = new System.Numerics.Vector2(((Vector4)triangle[0][VertexShader.PositionName]).X, ((Vector4)triangle[0][VertexShader.PositionName]).Y);
            System.Numerics.Vector2 t1 = new System.Numerics.Vector2(((Vector4)triangle[1][VertexShader.PositionName]).X, ((Vector4)triangle[1][VertexShader.PositionName]).Y);
            System.Numerics.Vector2 t2 = new System.Numerics.Vector2(((Vector4)triangle[2][VertexShader.PositionName]).X, ((Vector4)triangle[2][VertexShader.PositionName]).Y);

            System.Numerics.Vector2 v0 = t1 - t0;
            System.Numerics.Vector2 v1 = t2 - t0;
            System.Numerics.Vector2 v2 = pos - t0;

            float d00 = System.Numerics.Vector2.Dot(v0, v0);
            float d01 = System.Numerics.Vector2.Dot(v0, v1);
            float d11 = System.Numerics.Vector2.Dot(v1, v1);
            float d20 = System.Numerics.Vector2.Dot(v2, v0);
            float d21 = System.Numerics.Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1 - v - w;

            return new Vector3(u, v, w);
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Vector4ToInt
        {
            [FieldOffset(0)] public int ColorValue;
            [FieldOffset(0)] public byte valueB;
            [FieldOffset(1)] public byte valueG;
            [FieldOffset(2)] public byte valueR;
            [FieldOffset(3)] public byte valueA;
        }

        private Bitmap CalculateFragmentStep()
        {
            int[] imageData = new int[_fragments.GetLength(0) * _fragments.GetLength(1)];
            Vector4ToInt vec4ToInt = new Vector4ToInt();
            for (int x = 0; x < _fragments.GetLength(0); x++)
            {
                for (int y = 0; y < _fragments.GetLength(1); y++)
                {
                    if (_fragments[x, y] != null)
                    {
                        for (int i = 0; i < _fragments[x, y].Values.First().Count; i++)
                        {

                            bool closest = true;
                            if (DepthEnabled)
                            {
                                for (int j = 0; j < _depths[x, y].Count; j++)
                                {
                                    if (i != j)
                                    {
                                        if (_depths[x, y][i] > _depths[x, y][j])
                                        {
                                            closest = false;
                                        }
                                    }
                                }
                            }
                            if (closest)
                            {
                                foreach (var key in _fragments[x, y].Keys)
                                {
                                    SetAttribute(_activeFragmentShader, key, _fragments[x, y][key][i]);
                                }
                                _activeFragmentShader.Main();
                                foreach (var outValue in _activeFragmentShader.GetOutValues())
                                {
                                    if (outValue.Key == FragmentShader.ColorName)
                                    {
                                        vec4ToInt.ColorValue = 0;
                                        vec4ToInt.valueA = (byte)Math.Min((int)(((Vector4)outValue.Value).A * 255), 255);
                                        vec4ToInt.valueR = (byte)Math.Min((int)(((Vector4)outValue.Value).R * 255), 255);
                                        vec4ToInt.valueG = (byte)Math.Min((int)(((Vector4)outValue.Value).G * 255), 255);
                                        vec4ToInt.valueB = (byte)Math.Min((int)(((Vector4)outValue.Value).B * 255), 255);
                                        imageData[x + y * Width] = vec4ToInt.ColorValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return new Bitmap(Width, Height, Width, System.Drawing.Imaging.PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(imageData, 0));
        }
    }
}
