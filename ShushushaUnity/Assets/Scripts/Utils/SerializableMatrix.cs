// using System;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Utils
// {
//     [Serializable]
//     public class SerializableMatrix<T>
//     {
//         public int rows;
//         public int cols;
//
//         // 实际存储的数据
//         [SerializeField] protected T[] data;
//
//         // 构造函数：初始化数组大小
//         public SerializableMatrix(int r, int c)
//         {
//             rows = r;
//             cols = c;
//             data = new T[r * c];
//         }
//
//         public T this[int r, int c]
//         {
//             get => data[r * cols + c];
//             set => data[r * cols + c] = value;
//         }
//
//         public T[] this[int r]
//         {
//             get
//             {
//                 int start = r * cols;
//                 return data[start..(start + cols)];
//             }
//         }
//     }
//
//
//     public class SerializableMatrixConverter<T> : JsonConverter<SerializableMatrix<T>>
//     {
//         public override SerializableMatrix<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//         {
//             var arrays = JsonSerializer.Deserialize<T[][]>(ref reader, options);
//             if (arrays == null) return null;
//
//             int r = arrays.Length;
//             int c = r > 0 && arrays[0] != null ? arrays[0].Length : 0;
//
//             var matrix = new SerializableMatrix<T>(r, c);
//             for (int i = 0; i < r; i++)
//             {
//                 if (arrays[i] != null)
//                 {
//                     for (int j = 0; j < Mathf.Min(c, arrays[i].Length); j++)
//                     {
//                         matrix[i, j] = arrays[i][j];
//                     }
//                 }
//             }
//
//             return matrix;
//         }
//
//         public override void Write(Utf8JsonWriter writer, SerializableMatrix<T> value, JsonSerializerOptions options)
//         {
//             var arrays = new T[value.rows][];
//             for (int i = 0; i < value.rows; i++)
//             {
//                 arrays[i] = new T[value.cols];
//                 for (int j = 0; j < value.cols; j++)
//                 {
//                     arrays[i][j] = value[i, j];
//                 }
//             }
//
//             JsonSerializer.Serialize(writer, arrays, options);
//         }
//     }
// }