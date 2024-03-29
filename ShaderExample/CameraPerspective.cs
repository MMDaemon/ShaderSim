﻿using System;
using System.Numerics;

namespace ShaderExample
{
    public class CameraPerspective
    {
        private float _fovY;
        private Vector3 _position;

        public CameraPerspective()
        {
            Aspect = 1;
            FarClip = 100;
            FovY = 90;
            Jaw = 0;
            NearClip = 0.001f;
            Position = Vector3.Zero;
            Pitch = 0;
        }

        public float Aspect { get; set; }
        public float Jaw { get; set; }
        public float Pitch { get; set; }
        public float FarClip { get; set; }
        public float FovY
        {
            get => _fovY;
            set => _fovY = Clamp(value, 0f, 179.9f);
        }
        public float NearClip { get; set; }
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }
        public float PositionX
        {
            get => Position.X;
            set => _position.X = value;
        }
        public float PositionY
        {
            get => Position.Y;
            set => _position.Y = value;
        }
        public float PositionZ
        {
            get => Position.Z;
            set => _position.Z = value;
        }

        public Matrix4x4 CalcViewMatrix()
        {
            var mtxPitch = Matrix4x4.Transpose(Matrix4x4.CreateRotationX(DegreesToRadians(Pitch)));
            var mtxJaw = Matrix4x4.Transpose(Matrix4x4.CreateRotationY(DegreesToRadians(Jaw)));
            var mtxPosition = Matrix4x4.Transpose(Matrix4x4.CreateTranslation(-Position));
            return mtxPitch * mtxJaw * mtxPosition;
        }

        public Matrix4x4 CalcProjectionMatrix()
        {
            return Matrix4x4.Transpose(Matrix4x4.CreatePerspectiveFieldOfView(
                DegreesToRadians(FovY),
                Aspect, NearClip, FarClip));
        }

        public Matrix4x4 CalcMatrix()
        {
            return CalcProjectionMatrix() * CalcViewMatrix();
        }

        public Vector3 CalcPosition()
        {
            var view = CalcViewMatrix();
            if (!Matrix4x4.Invert(view, out Matrix4x4 inverse)) throw new ArithmeticException("Could not invert matrix");
            return new Vector3(inverse.M14, inverse.M24, inverse.M34);
        }

        float Clamp(float x, float min, float max)
        {
            return Math.Min(max, Math.Max(min, x));
        }

        private float DegreesToRadians(float angle)
        {
            return (float)((double)angle * (2 * Math.PI) / 360.0);
        }
    }
}
