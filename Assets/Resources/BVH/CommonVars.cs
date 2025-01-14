using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonVars {

    [System.Serializable]
    public struct LightData {
        public Vector3 Radiance;
        public Vector3 Position;
        public Vector3 Direction;
        public float energy;
        public float TotalEnergy;
        public int Type;
        public Vector2 SpotAngle;
    }



    [System.Serializable]
    public struct TriNodePairData {
        public int TriIndex;
        public int NodeIndex;
    }



    [System.Serializable]
    public struct MeshDat {
        public List<int> Indices;
        public List<Vector3> Verticies;
        public List<Vector3> Normals;
        public List<Vector3> Tangents;
        public List<Vector2> UVs;
        public List<int> MatDat;

        public void SetUvZero(int Count) {
            for(int i = 0; i < Count; i++) {
                UVs.Add(new Vector2(0.0f, 0.0f));
            }
        }
        public void init() {
            this.Tangents = new List<Vector3>();
            this.MatDat = new List<int>();
            this.UVs = new List<Vector2>();
            this.Verticies = new List<Vector3>();
            this.Normals = new List<Vector3>();
            this.Indices = new List<int>();
        }
        public void Clear() {
            this.Tangents.Clear();
            this.MatDat.Clear();
            this.UVs.Clear();
            this.Verticies.Clear();
            this.Normals.Clear();
            this.Indices.Clear();
        }
    }


    [System.Serializable]
    public struct MaterialData {
        public Vector4 AlbedoTex;
        public Vector4 NormalTex;
        public Vector4 EmissiveTex;
        public int HasAlbedoTex;
        public int HasNormalTex;
        public int HasEmissiveTex;
        public Vector3 BaseColor;
        public float emmissive;
        public float Roughness;
        public int MatType;
        public Vector3 eta;
    }

    [System.Serializable]
    public struct BVHNode2Data {
        public Vector3 BBMax;
        public Vector3 BBMin;
        public int left;    
        public int first;
        public uint count;
        public uint axis;
    }
    
    [System.Serializable]
    public struct BVHNode8Data {
        public Vector3 p;
        public uint[] e;
        public uint imask;    
        public uint base_index_child;
        public uint base_index_triangle;
        public uint[] meta;
        public uint[] quantized_min_x;
        public uint[] quantized_max_x;
        public uint[] quantized_min_y;
        public uint[] quantized_max_y;
        public uint[] quantized_min_z;
        public uint[] quantized_max_z;
    }

    [System.Serializable]
    public struct BVHNode8DataFixed {
        public Vector3 p;
        public uint e1;
        public uint e2;
        public uint e3;
        public uint imask;    
        public uint base_index_child;
        public uint base_index_triangle;
        public uint meta1;
        public uint meta2;
        public uint meta3;
        public uint meta4;
        public uint meta5;
        public uint meta6;
        public uint meta7;
        public uint meta8;
        public uint quantized_min_x1;
        public uint quantized_min_x2;
        public uint quantized_min_x3;
        public uint quantized_min_x4;
        public uint quantized_min_x5;
        public uint quantized_min_x6;
        public uint quantized_min_x7;
        public uint quantized_min_x8;
        public uint quantized_max_x1;
        public uint quantized_max_x2;
        public uint quantized_max_x3;
        public uint quantized_max_x4;
        public uint quantized_max_x5;
        public uint quantized_max_x6;
        public uint quantized_max_x7;
        public uint quantized_max_x8;
        public uint quantized_min_y1;
        public uint quantized_min_y2;
        public uint quantized_min_y3;
        public uint quantized_min_y4;
        public uint quantized_min_y5;
        public uint quantized_min_y6;
        public uint quantized_min_y7;
        public uint quantized_min_y8;
        public uint quantized_max_y1;
        public uint quantized_max_y2;
        public uint quantized_max_y3;
        public uint quantized_max_y4;
        public uint quantized_max_y5;
        public uint quantized_max_y6;
        public uint quantized_max_y7;
        public uint quantized_max_y8;
        public uint quantized_min_z1;
        public uint quantized_min_z2;
        public uint quantized_min_z3;
        public uint quantized_min_z4;
        public uint quantized_min_z5;
        public uint quantized_min_z6;
        public uint quantized_min_z7;
        public uint quantized_min_z8;
        public uint quantized_max_z1;
        public uint quantized_max_z2;
        public uint quantized_max_z3;
        public uint quantized_max_z4;
        public uint quantized_max_z5;
        public uint quantized_max_z6;
        public uint quantized_max_z7;
        public uint quantized_max_z8;
    }



    [System.Serializable]
    public struct PrimitiveData {
        public Vector3 BBMin;
        public Vector3 BBMax;
        public Vector3 Center;
        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        public Vector3 Norm1;
        public Vector3 Norm2;
        public Vector3 Norm3;
        public Vector3 Tan1;
        public Vector3 Tan2;
        public Vector3 Tan3;
        public Vector2 tex1;
        public Vector2 tex2;
        public Vector2 tex3;
        public int MatDat;

        public void Reconstruct() {
            BBMin = Vector3.Min(Vector3.Min(V1,V2),V3);
            BBMax = Vector3.Max(Vector3.Max(V1,V2),V3);
            for(int i2 = 0; i2 < 3; i2++) {
                if(BBMax[i2] - BBMin[i2] < 0.001f) {
                    BBMin[i2] -= 0.001f;
                    BBMax[i2] += 0.001f;
                }
            }
            Center = (V1 + V2 + V3) / 3.0f;
        }
    }

    public struct ProgReportData {
        public int Id;
        public string Name;
        public int TriCount;
        public void init(int Id, string Name, int TriCount) {
            this.Id = Id;
            this.Name = Name;
            this.TriCount = TriCount;
        }
    }
    
    [System.Serializable]
    public struct MyMeshDataCompacted {
        public Matrix4x4 Transform;
        public Matrix4x4 Inverse;
        public Vector3 Center;
        public int AggIndexCount;
        public int AggNodeCount;
        public int MaterialOffset;
        public int mesh_data_bvh_offsets;
        //I do have the space to store 1 more int and 1 more other value to align to 128 bits
    }

    [System.Serializable]
    public struct AABB {
        public Vector3 BBMax;
        public Vector3 BBMin;

        public void Extend(in Vector3 InMax, in Vector3 InMin) {
            this.BBMax = new Vector3(Mathf.Max(BBMax.x, InMax.x), Mathf.Max(BBMax.y, InMax.y), Mathf.Max(BBMax.z, InMax.z));
            this.BBMin = new Vector3(Mathf.Min(BBMin.x, InMin.x), Mathf.Min(BBMin.y, InMin.y), Mathf.Min(BBMin.z, InMin.z));
        }
        public void init() {
            BBMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            BBMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }
    }

        [System.Serializable]
    public struct NodeIndexPairData {
        public int PreviousNode;
        public int BVHNode;
        public int Node;
        public CommonVars.AABB AABB;
        public int InNodeOffset;
        public int IsLeaf;
        public int RecursionCount;
    }

    [System.Serializable]
    public struct BVHNode8DataCompressed {
        public Vector3 node_0xyz;
        public uint node_0w;
        public uint node_1x;
        public uint node_1y;
        public uint node_1z;
        public uint node_1w;
        public uint node_2x;
        public uint node_2y;
        public uint node_2z;
        public uint node_2w;
        public uint node_3x;
        public uint node_3y;
        public uint node_3z;
        public uint node_3w;
        public uint node_4x;
        public uint node_4y;
        public uint node_4z;
        public uint node_4w;
    }




    [System.Serializable]
    public struct CudaTriangle {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;

        public Vector3 norm0;
        public Vector3 normedge1;
        public Vector3 normedge2;

        public Vector3 tan0;
        public Vector3 tanedge1;
        public Vector3 tanedge2;

        public Vector2 tex0;
        public Vector2 texedge1;
        public Vector2 texedge2;

        public uint MatDat;
    }

    [System.Serializable]
    public struct CudaLightTriangle {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;
        public Vector3 Norm;

        public Vector3 radiance;
        public float sumEnergy;
        public float energy;
    }

    [System.Serializable]
    public struct LightMeshData {
        public Matrix4x4 Inverse;
        public Vector3 Center;
        public float energy;
        public float TotalEnergy;
        public int StartIndex;
        public int IndexEnd;
    }

    [System.Serializable]
    public struct Layer {
        public int[] Children;
        public int[] Leaf;

    }


    [System.Serializable]
    public struct SplitLayer {
        public int Child1;
        public int Child2;
        public int Child3;
        public int Child4;
        public int Child5;
        public int Child6;
        public int Child7;
        public int Child8;


        public int Leaf1;
        public int Leaf2;
        public int Leaf3;
        public int Leaf4;
        public int Leaf5;
        public int Leaf6;
        public int Leaf7;
        public int Leaf8;
    }

    [System.Serializable]
    public struct Layer2 {
        public List<int> Slab;
    }
    
}
