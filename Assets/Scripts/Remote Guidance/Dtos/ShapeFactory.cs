using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guidance.Dtos
{
    [Serializable]
    public class Shape3D
    {
        public ShapeType ShapeType { get; private set; }

        public Vector3[] PointData { get; private set; }

        [JsonIgnore]
        public Vector3 FirstPoint { get; private set; }

        [JsonIgnore]
        public Vector3 SecondPoint { get; private set; }

        public string Snapshot = string.Empty;

        public Shape3D(ShapeType shapeType, Vector3[] vectors, Vector3? endPosition = null)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors array cannot be null or empty.");

            ShapeType = shapeType;
            PointData = vectors;
            FirstPoint = vectors[0];
            SecondPoint = endPosition ?? vectors[vectors.Length - 1];
            Snapshot = DateTime.Now.Ticks.ToString();
        }
    }

    [Serializable]
    public class ShapeData
    {
        [JsonProperty("ShapeType")]
        public ShapeType ShapeType { get; set; }

        [JsonProperty("PointData")]
        public List<PointData> PointData { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        public ShapeData()
        {
            PointData = new List<PointData>();
        }

        [JsonIgnore]
        public PointData FirstPoint => PointData[0];

        [JsonIgnore]
        public PointData SecondPoint => PointData[(PointData.Count > 1) ? 1 : 0];

        [JsonIgnore]
        public PointData MidPoint1 => (PointData.Count == 1) ? PointData[0] : new PointData(FirstPoint.X, SecondPoint.Y);

        [JsonIgnore]
        public PointData MidPoint2 => (PointData.Count == 1) ? PointData[0] : new PointData(SecondPoint.X, FirstPoint.Y);
    }

    [Serializable]
    public class PointData
    {
        [JsonProperty("X")]
        public float X { get; set; }

        [JsonProperty("Y")]
        public float Y { get; set; }

        public PointData(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public enum ShapeType
    {
        Point,
        Line,
        Rect,
        None
    }

    // Factory Pattern for Shape Creation
    public class ShapeFactory
    {
        public static ShapeData CreateShape(string description, ShapeType shapeType, params float[] parameters)
        {
            var points = new List<PointData>();
            switch (shapeType)
            {
                case ShapeType.Point:
                    if (parameters.Length != 2)
                        throw new ArgumentException("Point requires 2 parameters: X, Y");

                    points.Add(new PointData(parameters[0], parameters[1]));

                    break;

                case ShapeType.Line:
                    if (parameters.Length != 4)
                        throw new ArgumentException("Line requires 4 parameters: X1, Y1, X2, Y2");

                    points.Add(new PointData(parameters[0], parameters[1]));
                    points.Add(new PointData(parameters[2], parameters[3]));

                    break;

                case ShapeType.Rect:
                    if (parameters.Length != 4)
                        throw new ArgumentException("Rect requires 4 parameters: X1, Y1, X2, Y2");

                    points.Add(new PointData(parameters[0], parameters[1]));
                    points.Add(new PointData(parameters[2], parameters[3]));

                    break;

                default:
                    throw new ArgumentException("Unsupported shape type");
            }

            return new ShapeData
            {
                Description = description,
                ShapeType = shapeType,
                PointData = points
            };
        }
    }
}