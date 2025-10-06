using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    // Implicit conversion from Unity's Vector3
    public static implicit operator SerializableVector3(Vector3 vector)
    {
        return new SerializableVector3(vector.x, vector.y, vector.z);
    }

    // Implicit conversion to Unity's Vector3
    public static implicit operator Vector3(SerializableVector3 vector)
    {
        return new Vector3(vector.x, vector.y, vector.z);
    }

    // Conversion from Unity.Mathematics.float3
    public static SerializableVector3 FromFloat3(Unity.Mathematics.float3 f3)
    {
        return new SerializableVector3(f3.x, f3.y, f3.z);
    }

    // Conversion to Unity.Mathematics.float3
    public Unity.Mathematics.float3 ToFloat3()
    {
        return new Unity.Mathematics.float3(x, y, z);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}