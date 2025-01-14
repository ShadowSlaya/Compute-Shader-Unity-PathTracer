using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;


[System.Serializable]
public class ParentObject : MonoBehaviour {


public ComputeBuffer TriBuffer;
public ComputeBuffer BVHBuffer;
public string Name;
public Texture2D AlbedoAtlas;
public Texture2D NormalAtlas;
public Texture2D EmissiveAtlas;
[HideInInspector] public RayTracingObject[] ChildObjects;
[HideInInspector] public bool MeshCountChanged;
[HideInInspector] public List<PrimitiveData> Triangles;
[HideInInspector] public CudaTriangle[] AggTriangles;
[HideInInspector] public List<CudaLightTriangle> LightTriangles;
[HideInInspector] public BVH8Builder BVH;
[HideInInspector] public SkinnedMeshRenderer[] SkinnedMeshes;
[HideInInspector] public int[] IndexCounts;
public GraphicsBuffer FinalMeshBuffer;
[HideInInspector] public ComputeShader MeshRefit;
[HideInInspector] public bool HasStarted;
[HideInInspector] public BVHNode8DataCompressed[] AggNodes;
public AABB aabb_untransformed;
public AABB aabb;
[HideInInspector] public int AggIndexCount;
[HideInInspector] public int AggBVHNodeCount;
[HideInInspector] public int StaticBVHOffset;
[HideInInspector] public bool ObjectOrderHasChanged;
[HideInInspector] public List<MaterialData> _Materials;
[HideInInspector] public int MatOffset;
[HideInInspector] public int InstanceID;
[HideInInspector] public StorableTransform[] CachedTransforms;
[HideInInspector] public MeshDat CurMeshData;
[HideInInspector] public int TotalObjects;
[HideInInspector] public List<MeshTransformVertexs> TransformIndexes;
[HideInInspector] public bool HasCompleted;
[HideInInspector] public float TotEnergy;
[HideInInspector] public int LightCount;

[HideInInspector] public int ConstructKernel;
[HideInInspector] public int RemeshKernel;
[HideInInspector] public int RefitLayerKernel;
[HideInInspector] public int TriConvertKernel;
[HideInInspector] public int NodeUpdateKernel;
[HideInInspector] public int NodeCompressKernel;
[HideInInspector] public int NodeInitializerKernel;


[HideInInspector] public bool HasAlbedoAtlas;
[HideInInspector] public bool HasNormalAtlas;
[HideInInspector] public bool HasEmissiveAtlas;

[HideInInspector] public int TotalTriangles;
[HideInInspector] public bool IsSkinnedGroup;

public ComputeBuffer NodeBuffer;
public ComputeBuffer AdvancedTriangleBuffer;
public ComputeBuffer StackBuffer;
public ComputeBuffer VertexBufferOut;

public ComputeBuffer BVHDataBuffer;
public ComputeBuffer ToBVHIndexBuffer;
public ComputeBuffer CWBVHIndicesBuffer;

[HideInInspector] public List<Layer> ForwardStack;
[HideInInspector] public Layer2[] LayerStack;
[HideInInspector] public List<SplitLayer> SplitForwardStack;
[HideInInspector] public bool started = false;

[HideInInspector] public List<NodeIndexPairData> NodePair;
[HideInInspector] public List<TriNodePairData> TriPair;
[HideInInspector] public int MaxRecur = 0;
[HideInInspector] public int[] ToBVHIndex;

[HideInInspector] public int NodeOffset;
[HideInInspector] public int TriOffset;

[System.Serializable]
public struct StorableTransform {
    public Matrix4x4 WTL;
    public Vector3 Position;
}

[System.Serializable]
public struct MeshTransformVertexs {
    public int VertexStart;
    public int VertexCount;
}

[System.Serializable]
public struct PerMatTextureData {
    public bool HasAlbedoMap;
    public bool HasNormalMap;
    public bool HasEmissiveMap;
    public RayTracingObject MaterialObject;
}
public List<PerMatTextureData> MatTexData;


public void ClearAll() {
    Debug.Log("CLEAR");
    Triangles.Clear();
    Triangles.TrimExcess();
    LightTriangles.Clear();
    LightTriangles.TrimExcess();
    BVH = null;
    _Materials.Clear();
    _Materials.TrimExcess();
    CurMeshData.Clear();
    TransformIndexes = null;
    MeshCountChanged = true;
    HasCompleted = false;
    DestroyImmediate(AlbedoAtlas);
    DestroyImmediate(NormalAtlas);
    DestroyImmediate(EmissiveAtlas); 
}

public void init() {
    InstanceID = this.GetInstanceID();
    Name = this.name;
    TransformIndexes = new List<MeshTransformVertexs>();
    _Materials = new List<MaterialData>();
    Triangles = new List<PrimitiveData>();
    LightTriangles = new List<CudaLightTriangle>();
    MeshCountChanged = true;
    ObjectOrderHasChanged = false;
    HasCompleted = false;
    MeshRefit =  Resources.Load<ComputeShader>("BVH/BVHRefitter");
    ConstructKernel = MeshRefit.FindKernel("Construct");
    RemeshKernel = MeshRefit.FindKernel("Remesh");
    RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
    TriConvertKernel = MeshRefit.FindKernel("TriFormatConvert");
    NodeUpdateKernel = MeshRefit.FindKernel("NodeUpdate");
    NodeCompressKernel = MeshRefit.FindKernel("NodeCompress");
    NodeInitializerKernel = MeshRefit.FindKernel("NodeInitializer");

}
public void SetUpBuffers() {
    TriBuffer = new ComputeBuffer(AggTriangles.Length, 136);
    BVHBuffer = new ComputeBuffer(AggNodes.Length, 80);
    TriBuffer.SetData(AggTriangles);
    BVHBuffer.SetData(AggNodes);
}


public struct objtextureindices {
    public int textureindexstart;
    public int textureindexcount;
}

private void CreateAtlas() {//Creates texture atlas
    MatTexData = new List<PerMatTextureData>();
    _Materials.Clear();
    List<Texture2D> AlbedoTexs = new List<Texture2D>();
    List<Texture2D> NormalTexs = new List<Texture2D>();
    List<Texture2D> EmissiveTexs = new List<Texture2D>();
    AlbedoAtlas = null;
    NormalAtlas = null;
    EmissiveAtlas = null;
    Mesh mesh = new Mesh();
    PerMatTextureData CurrentTexDat = new PerMatTextureData();
    foreach(RayTracingObject obj in ChildObjects) {
        if(obj.GetComponent<MeshFilter>() != null) { 
            mesh = obj.GetComponent<MeshFilter>().sharedMesh;
        }else {
            mesh = obj.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        }
        Material[] SharedMaterials = (obj.GetComponent<Renderer>() != null) ? obj.GetComponent<Renderer>().sharedMaterials : obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials;       
        int SharedMatLength = SharedMaterials.Length;
        for(int i = 0; i < SharedMatLength; ++i) {
            CurrentTexDat.MaterialObject = obj;
            if(SharedMaterials[Mathf.Min(i, SharedMatLength - 1)] == null) {
                i--;
                SharedMatLength--;
            }
            if(SharedMaterials[i].mainTexture != null) {
                AlbedoTexs.Add((Texture2D)SharedMaterials[i].mainTexture);
                CurrentTexDat.HasAlbedoMap = true;
            } else {
                CurrentTexDat.HasAlbedoMap = false;
            }
            if(SharedMaterials[i].GetTexture("_BumpMap") != null) {
                NormalTexs.Add((Texture2D)SharedMaterials[i].GetTexture("_BumpMap"));
                CurrentTexDat.HasNormalMap = true;
            } else {
                CurrentTexDat.HasNormalMap = false;
            }
            if(SharedMaterials[i].GetTexture("_EmissionMap") != null) {
                EmissiveTexs.Add((Texture2D)SharedMaterials[i].GetTexture("_EmissionMap"));
                CurrentTexDat.HasEmissiveMap = true;
            } else {
                CurrentTexDat.HasEmissiveMap = false;
            }
            MatTexData.Add(CurrentTexDat);


        }
    }
    int AlbedoCount = 0;
    int NormalCount = 0;
    int EmissiveCount = 0;
    Rect[] AlbedoRects;
    Rect[] NormalRects;
    Rect[] EmmissiveRects;
    if(AlbedoTexs.Count != 0) {
        AlbedoAtlas = new Texture2D(8192, 8192);
        AlbedoRects = AlbedoAtlas.PackTextures(AlbedoTexs.ToArray(), 2, 8192);
        HasAlbedoAtlas = true;
    } else {
        HasAlbedoAtlas = false;
        AlbedoAtlas = new Texture2D(1,1);
        AlbedoRects = new Rect[0];
    }
    if(NormalTexs.Count != 0) {
        NormalAtlas = new Texture2D(8192, 8192);
        NormalRects = NormalAtlas.PackTextures(NormalTexs.ToArray(), 0, 8192);
        HasNormalAtlas = true;
    } else {
        HasNormalAtlas = false;
        NormalAtlas = new Texture2D(1,1);
        NormalRects = new Rect[0];
    }
    if(EmissiveTexs.Count != 0) {
        EmissiveAtlas = new Texture2D(2048, 2048);
        EmmissiveRects = EmissiveAtlas.PackTextures(EmissiveTexs.ToArray(), 2, 2048);
        HasEmissiveAtlas = true;
    } else {
        HasEmissiveAtlas = false;
        EmissiveAtlas = new Texture2D(1,1);
        EmmissiveRects = new Rect[0];
    }
    AlbedoTexs.Clear();
    AlbedoTexs.TrimExcess();
    NormalTexs.Clear();
    NormalTexs.TrimExcess();
    EmissiveTexs.Clear();
    EmissiveTexs.TrimExcess();
    RayTracingObject PreviousObject = MatTexData[0].MaterialObject;
    int CurrentObjectOffset = -1;
    Vector4 AlbedoTex = new Vector4(0,0,0,0);
    Vector4 NormalTex = new Vector4(0,0,0,0);
    Vector4 EmissiveTex = new Vector4(0,0,0,0);
    int CurMat = 0;
    foreach(PerMatTextureData Obj in MatTexData) {
        if(PreviousObject.Equals(Obj.MaterialObject)) {
            CurrentObjectOffset++;
        } else {
            CurrentObjectOffset = 0;
        }
        if(Obj.HasAlbedoMap) {
            AlbedoTex = new Vector4(AlbedoRects[AlbedoCount].xMax, AlbedoRects[AlbedoCount].yMax, AlbedoRects[AlbedoCount].xMin, AlbedoRects[AlbedoCount].yMin);
            AlbedoCount++;
        }
        if(Obj.HasNormalMap) {
            NormalTex = new Vector4(NormalRects[NormalCount].xMax, NormalRects[NormalCount].yMax, NormalRects[NormalCount].xMin, NormalRects[NormalCount].yMin);
            NormalCount++;
        }
        if(Obj.HasEmissiveMap) {
            EmissiveTex = new Vector4(EmmissiveRects[EmissiveCount].xMax, EmmissiveRects[EmissiveCount].yMax, EmmissiveRects[EmissiveCount].xMin, EmmissiveRects[EmissiveCount].yMin);
            EmissiveCount++;
        }
        _Materials.Add(new MaterialData() {
            AlbedoTex = AlbedoTex,
            NormalTex = NormalTex,
            EmissiveTex = EmissiveTex,
            HasAlbedoTex = (Obj.HasAlbedoMap) ? 1 : 0,
            HasNormalTex = (Obj.HasNormalMap) ? 1 : 0,
            HasEmissiveTex = (Obj.HasEmissiveMap) ? 1 : 0,
            BaseColor = Obj.MaterialObject.BaseColor[CurrentObjectOffset],
            emmissive = Obj.MaterialObject.emmission[CurrentObjectOffset],
            Roughness = Obj.MaterialObject.Roughness[CurrentObjectOffset],
            MatType = Obj.MaterialObject.MatType[CurrentObjectOffset],
            eta = Obj.MaterialObject.eta[CurrentObjectOffset]
        });
        Obj.MaterialObject.MaterialIndex[CurrentObjectOffset] = CurMat;
        Obj.MaterialObject.LocalMaterialIndex[CurrentObjectOffset] = CurMat;
        PreviousObject = Obj.MaterialObject;
        CurMat++;
    }

}


public void LoadData() {
    TotEnergy = 0;
    LightCount = 0;
    init();
    Triangles.Clear();
    CurMeshData = new MeshDat();
    CurMeshData.init();
    List<RayTracingObject> TempObjects = new List<RayTracingObject>();
    List<Transform> TempObjectTransforms = new List<Transform>();
    TempObjectTransforms.Add(this.transform);
    IsSkinnedGroup = false;
    for(int i = 0; i < this.transform.childCount; i++) {
        if(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>() != null && this.transform.GetChild(i).gameObject.activeInHierarchy) {
            TempObjectTransforms.Add(this.transform.GetChild(i));
            TempObjects.Add(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>());
            if(this.transform.GetChild(i).gameObject.GetComponent<SkinnedMeshRenderer>() != null) IsSkinnedGroup = true;
        }
    }
    Transform[] TempTransforms = TempObjectTransforms.ToArray();
    CachedTransforms = new StorableTransform[TempTransforms.Length];
    for(int i = 0; i < TempTransforms.Length; i++) {
        CachedTransforms[i].WTL = TempTransforms[i].worldToLocalMatrix;
        CachedTransforms[i].Position = TempTransforms[i].position;
    }
    TempObjectTransforms.Clear();
    TempObjectTransforms.Capacity = 0;
    ChildObjects = TempObjects.ToArray();
    TempObjects.Clear();
    TempObjects.Capacity = 0;
    TotalObjects = ChildObjects.Length;
    if(IsSkinnedGroup) {
        HasStarted = false;
        SkinnedMeshes = new SkinnedMeshRenderer[TotalObjects];
        TotalTriangles = 0;
        IndexCounts = new int[TotalObjects];
        for(int i = 0; i < TotalObjects; i++) {
            SkinnedMeshes[i] = ChildObjects[i].GetComponent<SkinnedMeshRenderer>();
            SkinnedMeshes[i].updateWhenOffscreen = true;
            int CurrentTriangleCount = 0;
            for(int i2 = 0; i2 < SkinnedMeshes[i].sharedMesh.subMeshCount; i2++) {
                CurrentTriangleCount += SkinnedMeshes[i].sharedMesh.GetIndices(i2).Length / 3;
            }
            TotalTriangles += CurrentTriangleCount;
            SkinnedMeshes[i].sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            IndexCounts[i] = (int)Mathf.Ceil(CurrentTriangleCount);
        }
    }
    CreateAtlas();
    int submeshcount;
    Mesh mesh;
    RayTracingObject CurrentObject;
    int MatIndex = 0;
    int RepCount = 0;
    this.MatOffset = _Materials.Count;
    for(int i = 0; i < TotalObjects; i++) {
        CurrentObject = ChildObjects[i];
            mesh = new Mesh();
            if(CurrentObject.GetComponent<MeshFilter>() != null) { 
                mesh = CurrentObject.GetComponent<MeshFilter>().sharedMesh;
            }else {
                CurrentObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh, true);
            }
            submeshcount = mesh.subMeshCount;
            
            List<Vector4> Tans = new List<Vector4>();
            mesh.GetTangents(Tans);
            for(int i2 = 0; i2 < Tans.Count; i2++) {
                CurMeshData.Tangents.Add(new Vector3(Tans[i2].x, Tans[i2].y, Tans[i2].z));
            }
        
            List<Vector3> Norms = new List<Vector3>();
            mesh.GetNormals(Norms);
            CurMeshData.Normals.AddRange(Norms);
            int IndexOffset = CurMeshData.Verticies.Count;
            CurMeshData.Verticies.AddRange(mesh.vertices);
            int MeshUvLength = mesh.uv.Length;
            if(MeshUvLength != 0) {
                CurMeshData.UVs.AddRange(mesh.uv);
            } else {
                CurMeshData.SetUvZero(MeshUvLength);
            }
            int PreIndexLength = CurMeshData.Indices.Count;
            for(int i2 = 0; i2 < submeshcount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                int PrevLength = CurMeshData.Indices.Count;
                List<int> NewIndexes = new List<int>(mesh.GetIndices(i2));
                int NewIndexLength = NewIndexes.Count;
                for(int i3 = 0; i3 < NewIndexLength; i3++) {
                    CurMeshData.Indices.Add(NewIndexes[i3] + IndexOffset);    
                }
                int IndiceLength = (CurMeshData.Indices.Count - PrevLength) / 3;
                MatIndex = i2 + RepCount;
                for(int i3 = 0; i3 < IndiceLength; ++i3) {
                    CurMeshData.MatDat.Add(MatIndex);
                }
            }
            TransformIndexes.Add(new MeshTransformVertexs() {
                VertexStart = PreIndexLength,
                VertexCount = CurMeshData.Indices.Count - PreIndexLength
            });
            RepCount += submeshcount;
        }
}



int NumberOfSetBits(int i)
{
    i = i - ((i >> 1) & 0x55555555);
    i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
    return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
}


public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur) {
    NodeIndexPairData CurrentPair = NodePair[CurrentNode];
    MaxRecur = Mathf.Max(MaxRecur, CurRecur);
    CurrentPair.PreviousNode = ParentNode;
    CurrentPair.Node = CurrentNode;
    CurrentPair.RecursionCount = CurRecur;
    if(!IsLeafRecur) {
        ToBVHIndex[NextBVH8Node] = CurrentNode;
        CurrentPair.IsLeaf = 0;
        BVHNode8Data node = BVH.BVH8Nodes[NextBVH8Node];
        NodeIndexPairData IndexPair = new NodeIndexPairData();

        IndexPair.AABB = new AABB();
        float ex = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[0]) << 23));
        float ey = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[1]) << 23));
        float ez = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[2]) << 23));
        Vector3 e = new Vector3(ex, ey, ez);
        for(int i = 0; i < 8; i++) {
            IndexPair.InNodeOffset = i;
            float AABBPos1x = node.quantized_max_x[i] * e.x + node.p.x;
            float AABBPos1y = node.quantized_max_y[i] * e.y + node.p.y;
            float AABBPos1z = node.quantized_max_z[i] * e.z + node.p.z;
            float AABBPos2x = node.quantized_min_x[i] * e.x + node.p.x;
            float AABBPos2y = node.quantized_min_y[i] * e.y + node.p.y;
            float AABBPos2z = node.quantized_min_z[i] * e.z + node.p.z;
            IndexPair.AABB.init();
            IndexPair.AABB.Extend(new Vector3(AABBPos1x, AABBPos1y, AABBPos1z), new Vector3(AABBPos2x, AABBPos2y, AABBPos2z));
            IndexPair.InNodeOffset = i;
            bool IsLeaf = (node.meta[i] & 0b11111) < 24;
           if(IsLeaf) {
                int first_triangle = (byte)node.meta[i] & 0b11111;
                int NumBits = NumberOfSetBits((byte)node.meta[i] >> 5);
                for (int j = 0; j < NumBits; j++) {
                    TriPair.Add(new TriNodePairData() {
                        TriIndex = (int)node.base_index_triangle + first_triangle + j,
                        NodeIndex = NodePair.Count
                        });
                }
                IndexPair.BVHNode = NextBVH8Node;
                NodePair.Add(IndexPair);
                NextNode++;
                DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, -1, true, CurRecur + 1);

            } else {
                int child_offset = (byte)node.meta[i] & 0b11111;
                int child_index  = (int)node.base_index_child + child_offset - 24;
                
                IndexPair.BVHNode = NextBVH8Node;
                NodePair.Add(IndexPair);
                NextNode++;
                DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, child_index, false, CurRecur + 1);
            }
        }
    } else {
        CurrentPair.IsLeaf = 1;
    }
    NodePair[CurrentNode] = CurrentPair;
}


private string IntListToString(ref List<int> a) {
string Out = "";
for(int i = 0; i < a.Count; i++) {
    Out += a[i] + ", ";
}
return Out;
}

private int[] IntArray(int[] a) {
return (new List<int>(a)).ToArray();
}



public void Construct() {
    MaxRecur = 0;
    started = false;
    SplitForwardStack = new List<SplitLayer>();
    BVH2Builder BVH2 = new BVH2Builder(Triangles);//Binary BVH Builder, and also the component that takes the longest to build
    this.BVH = new BVH8Builder(ref BVH2);
    BVH2 = null;
    BVH.BVH8Nodes.RemoveRange(BVH.cwbvhnode_count, BVH.BVH8Nodes.Count - BVH.cwbvhnode_count);
    BVH.BVH8Nodes.Capacity = BVH.BVH8Nodes.Count;
    ToBVHIndex = new int[BVH.BVH8Nodes.Count];
    
    if(IsSkinnedGroup) {
        NodePair = new List<NodeIndexPairData>();
        TriPair = new List<TriNodePairData>();
        NodePair.Add(new NodeIndexPairData());
        DocumentNodes(0, 0, 1, 0, false, 0);
        MaxRecur++;
        int NodeCount = NodePair.Count;
        Layer ForwardStackNodeT = new Layer();
        ForwardStack = new List<Layer>();
        ForwardStackNodeT.Children = new int[]{-1,-1,-1,-1,-1,-1,-1,-1};
        ForwardStackNodeT.Leaf = new int[]{-1,-1,-1,-1,-1,-1,-1,-1};
        for(int i = 0; i < NodePair.Count; i++) {
            ForwardStack.Add(ForwardStackNodeT);
        }

        for(int i = 0; i < NodePair.Count; i++) {
            Layer ForwardStackNode = new Layer();
            ForwardStackNode.Children = IntArray(ForwardStack[i].Children);
            ForwardStackNode.Leaf = IntArray(ForwardStack[i].Leaf);
            if(NodePair[i].IsLeaf == 1) {
                int first_triangle = (byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                int NumBits = NumberOfSetBits((byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                ForwardStackNode.Children[NodePair[i].InNodeOffset] = NumBits;
                ForwardStackNode.Leaf[NodePair[i].InNodeOffset] = (int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1; 
            } else {
                ForwardStackNode.Children[NodePair[i].InNodeOffset] = i;
                ForwardStackNode.Leaf[NodePair[i].InNodeOffset] = 0;
            }
            ForwardStack[i] = ForwardStackNode;
        }


        for(int i = 0; i < NodePair.Count; i++) {
            Layer ForwardStackNode = new Layer();
            ForwardStackNode.Children = IntArray(ForwardStack[NodePair[i].PreviousNode].Children);
            ForwardStackNode.Leaf = IntArray(ForwardStack[NodePair[i].PreviousNode].Leaf);
            ForwardStackNode.Children[NodePair[i].InNodeOffset] = i;
            ForwardStackNode.Leaf[NodePair[i].InNodeOffset] = 0;
            ForwardStack[NodePair[i].PreviousNode] = ForwardStackNode;
        }
        
        LayerStack = new Layer2[MaxRecur];
        for(int i = 0; i < MaxRecur; i++) {
           Layer2 TempSlab = new Layer2();
            TempSlab.Slab = new List<int>();
            LayerStack[i] = TempSlab;
        }
        Debug.Log("Done Stage 1");
        for(int i = 0; i < NodePair.Count; i++) {
            var TempLayer = LayerStack[NodePair[i].RecursionCount];
            TempLayer.Slab.Add(i);
            LayerStack[NodePair[i].RecursionCount] = TempLayer;
        }
        Debug.Log("Done Stage 2");

        SplitForwardStack = new List<SplitLayer>();
        for(int i = 0; i < ForwardStack.Count; i++) {
            SplitLayer TempSplit = new SplitLayer();
            TempSplit.Child1 = ForwardStack[i].Children[0];
            TempSplit.Child2 = ForwardStack[i].Children[1];
            TempSplit.Child3 = ForwardStack[i].Children[2];
            TempSplit.Child4 = ForwardStack[i].Children[3];
            TempSplit.Child5 = ForwardStack[i].Children[4];
            TempSplit.Child6 = ForwardStack[i].Children[5];
            TempSplit.Child7 = ForwardStack[i].Children[6];
            TempSplit.Child8 = ForwardStack[i].Children[7];
            
            TempSplit.Leaf1 = ForwardStack[i].Leaf[0];
            TempSplit.Leaf2 = ForwardStack[i].Leaf[1];
            TempSplit.Leaf3 = ForwardStack[i].Leaf[2];
            TempSplit.Leaf4 = ForwardStack[i].Leaf[3];
            TempSplit.Leaf5 = ForwardStack[i].Leaf[4];
            TempSplit.Leaf6 = ForwardStack[i].Leaf[5];
            TempSplit.Leaf7 = ForwardStack[i].Leaf[6];
            TempSplit.Leaf8 = ForwardStack[i].Leaf[7];

            SplitForwardStack.Add(TempSplit);
        }
        ConvertToSplitNodes();
    }
}





public List<BVHNode8DataFixed> SplitNodes;
private void ConvertToSplitNodes() {
BVHNode8DataFixed NewNode = new BVHNode8DataFixed();
SplitNodes = new List<BVHNode8DataFixed>();
BVHNode8Data SourceNode;
for(int i = 0; i < BVH.BVH8Nodes.Count; i++) {
    SourceNode = BVH.BVH8Nodes[i];
    NewNode.p = SourceNode.p;
    NewNode.e1 = SourceNode.e[0];
    NewNode.e2 = SourceNode.e[1];
    NewNode.e3 = SourceNode.e[2];
    NewNode.imask = SourceNode.imask;
    NewNode.base_index_child = SourceNode.base_index_child;
    NewNode.base_index_triangle = SourceNode.base_index_triangle;
    NewNode.meta1 = SourceNode.meta[0];
    NewNode.meta2 = SourceNode.meta[1];
    NewNode.meta3 = SourceNode.meta[2];
    NewNode.meta4 = SourceNode.meta[3];
    NewNode.meta5 = SourceNode.meta[4];
    NewNode.meta6 = SourceNode.meta[5];
    NewNode.meta7 = SourceNode.meta[6];
    NewNode.meta8 = SourceNode.meta[7];
    NewNode.quantized_min_x1 = SourceNode.quantized_min_x[0];
    NewNode.quantized_min_x2 = SourceNode.quantized_min_x[1];
    NewNode.quantized_min_x3 = SourceNode.quantized_min_x[2];
    NewNode.quantized_min_x4 = SourceNode.quantized_min_x[3];
    NewNode.quantized_min_x5 = SourceNode.quantized_min_x[4];
    NewNode.quantized_min_x6 = SourceNode.quantized_min_x[5];
    NewNode.quantized_min_x7 = SourceNode.quantized_min_x[6];
    NewNode.quantized_min_x8 = SourceNode.quantized_min_x[7];
    NewNode.quantized_max_x1 = SourceNode.quantized_max_x[0];
    NewNode.quantized_max_x2 = SourceNode.quantized_max_x[1];
    NewNode.quantized_max_x3 = SourceNode.quantized_max_x[2];
    NewNode.quantized_max_x4 = SourceNode.quantized_max_x[3];
    NewNode.quantized_max_x5 = SourceNode.quantized_max_x[4];
    NewNode.quantized_max_x6 = SourceNode.quantized_max_x[5];
    NewNode.quantized_max_x7 = SourceNode.quantized_max_x[6];
    NewNode.quantized_max_x8 = SourceNode.quantized_max_x[7];

    NewNode.quantized_min_y1 = SourceNode.quantized_min_y[0];
    NewNode.quantized_min_y2 = SourceNode.quantized_min_y[1];
    NewNode.quantized_min_y3 = SourceNode.quantized_min_y[2];
    NewNode.quantized_min_y4 = SourceNode.quantized_min_y[3];
    NewNode.quantized_min_y5 = SourceNode.quantized_min_y[4];
    NewNode.quantized_min_y6 = SourceNode.quantized_min_y[5];
    NewNode.quantized_min_y7 = SourceNode.quantized_min_y[6];
    NewNode.quantized_min_y8 = SourceNode.quantized_min_y[7];
    NewNode.quantized_max_y1 = SourceNode.quantized_max_y[0];
    NewNode.quantized_max_y2 = SourceNode.quantized_max_y[1];
    NewNode.quantized_max_y3 = SourceNode.quantized_max_y[2];
    NewNode.quantized_max_y4 = SourceNode.quantized_max_y[3];
    NewNode.quantized_max_y5 = SourceNode.quantized_max_y[4];
    NewNode.quantized_max_y6 = SourceNode.quantized_max_y[5];
    NewNode.quantized_max_y7 = SourceNode.quantized_max_y[6];
    NewNode.quantized_max_y8 = SourceNode.quantized_max_y[7];

    NewNode.quantized_min_z1 = SourceNode.quantized_min_z[0];
    NewNode.quantized_min_z2 = SourceNode.quantized_min_z[1];
    NewNode.quantized_min_z3 = SourceNode.quantized_min_z[2];
    NewNode.quantized_min_z4 = SourceNode.quantized_min_z[3];
    NewNode.quantized_min_z5 = SourceNode.quantized_min_z[4];
    NewNode.quantized_min_z6 = SourceNode.quantized_min_z[5];
    NewNode.quantized_min_z7 = SourceNode.quantized_min_z[6];
    NewNode.quantized_min_z8 = SourceNode.quantized_min_z[7];
    NewNode.quantized_max_z1 = SourceNode.quantized_max_z[0];
    NewNode.quantized_max_z2 = SourceNode.quantized_max_z[1];
    NewNode.quantized_max_z3 = SourceNode.quantized_max_z[2];
    NewNode.quantized_max_z4 = SourceNode.quantized_max_z[3];
    NewNode.quantized_max_z5 = SourceNode.quantized_max_z[4];
    NewNode.quantized_max_z6 = SourceNode.quantized_max_z[5];
    NewNode.quantized_max_z7 = SourceNode.quantized_max_z[6];
    NewNode.quantized_max_z8 = SourceNode.quantized_max_z[7];

    SplitNodes.Add(NewNode);
}
}


public void RefitMesh() {
    int KernelRatio = 256;

    AABB OverAABB = new AABB();
    OverAABB.init();
    for(int i = 0; i < SkinnedMeshes.Length; i++) {
        Vector3 V2 = SkinnedMeshes[i].bounds.center + SkinnedMeshes[i].bounds.size / 2.0f;
        Vector3 V3 = SkinnedMeshes[i].bounds.center - SkinnedMeshes[i].bounds.size / 2.0f;
        OverAABB.Extend(V2, V3);
    }

    aabb = OverAABB;
    if(!HasStarted) {
        NodeBuffer = new ComputeBuffer(NodePair.Count, 48);
        NodeBuffer.SetData(NodePair);
        AdvancedTriangleBuffer = new ComputeBuffer(TotalTriangles, 96);
        VertexBufferOut = new ComputeBuffer(TotalTriangles, 72);
        StackBuffer = new ComputeBuffer(ForwardStack.Count, 64);
        StackBuffer.SetData(SplitForwardStack);
        CWBVHIndicesBuffer = new ComputeBuffer(BVH.cwbvh_indices.Count, 4);
        CWBVHIndicesBuffer.SetData(BVH.cwbvh_indices.ToArray()); 
        BVHDataBuffer = new ComputeBuffer(AggNodes.Length, 260);
        BVHDataBuffer.SetData(SplitNodes); 
        ToBVHIndexBuffer = new ComputeBuffer(ToBVHIndex.Length, 4);
        ToBVHIndexBuffer.SetData(ToBVHIndex);     
        HasStarted = true;
        Debug.Log("STARTED");
    } else {
        MeshRefit.SetBuffer(RefitLayerKernel, "ReverseStack", StackBuffer);

     int CurVertOffset = 0;
     for(int i = 0; i < TotalObjects; i++) {
        var TempOffset = CurVertOffset;
        int IndexCount = IndexCounts[i];
        MeshRefit.SetInt("VertOffset", TempOffset);
        MeshRefit.SetInt("gVertexCount", IndexCount);
        SkinnedMeshes[i].sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        GraphicsBuffer IndexBuffer2 = SkinnedMeshes[i].sharedMesh.GetIndexBuffer();
        SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        FinalMeshBuffer = SkinnedMeshes[i].GetVertexBuffer();
        MeshRefit.SetMatrix("Transform", SkinnedMeshes[i].bones[0].transform.worldToLocalMatrix);
        MeshRefit.SetMatrix("Transform2", this.transform.worldToLocalMatrix.inverse);
        MeshRefit.SetMatrix("Transform3", this.transform.worldToLocalMatrix);
        MeshRefit.SetVector("Scale", this.transform.lossyScale);
        MeshRefit.SetVector("Offset", SkinnedMeshes[i].bones[0].parent.transform.worldToLocalMatrix.inverse * SkinnedMeshes[i].bones[0].localPosition);
      if(FinalMeshBuffer != null)   MeshRefit.SetBuffer(ConstructKernel, "bufVertices", FinalMeshBuffer);
        MeshRefit.SetBuffer(ConstructKernel, "bufIndexes", IndexBuffer2);
        MeshRefit.SetBuffer(ConstructKernel, "VertexsOut", VertexBufferOut);
      if(FinalMeshBuffer != null)   MeshRefit.Dispatch(ConstructKernel, (int)Mathf.Ceil(IndexCount / (float)KernelRatio),1,1);
        CurVertOffset += IndexCount;
        if(FinalMeshBuffer != null) FinalMeshBuffer.Dispose();
        IndexBuffer2.Dispose();

    }

    MeshRefit.SetInt("gVertexCount", TotalTriangles);
    MeshRefit.SetBuffer(RemeshKernel, "CWBVHIndices", CWBVHIndicesBuffer);
    MeshRefit.SetBuffer(RemeshKernel, "VertexsIn", VertexBufferOut);
    MeshRefit.SetBuffer(RemeshKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
    MeshRefit.Dispatch(RemeshKernel, (int)Mathf.Ceil(TotalTriangles / (float)KernelRatio),1,1);

    MeshRefit.SetInt("NodeCount", TotalTriangles);
    MeshRefit.SetBuffer(TriConvertKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
    MeshRefit.SetBuffer(TriConvertKernel, "CudaTriArray", TriBuffer);
    MeshRefit.Dispatch(TriConvertKernel, (int)Mathf.Ceil(TotalTriangles / (float)KernelRatio),1,1);

    MeshRefit.SetInt("NodeCount", NodePair.Count);
    MeshRefit.SetBuffer(NodeInitializerKernel, "AllNodes", NodeBuffer);
    MeshRefit.Dispatch(NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);

    MeshRefit.SetBuffer(RefitLayerKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
    for(int i = MaxRecur - 1; i >= 0; i--) {
        var NodeCount2 = LayerStack[i].Slab.Count;
        ComputeBuffer WorkingBuffer = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
        WorkingBuffer.SetData(LayerStack[i].Slab);
        MeshRefit.SetInt("NodeCount", NodeCount2);
        MeshRefit.SetBuffer(RefitLayerKernel, "AllNodes", NodeBuffer);
        MeshRefit.SetBuffer(RefitLayerKernel, "NodesToWork", WorkingBuffer);        
        MeshRefit.Dispatch(RefitLayerKernel, (int)Mathf.Ceil(WorkingBuffer.count / (float)KernelRatio), 1, 1);
        WorkingBuffer.Dispose();
    }

    MeshRefit.SetInt("NodeCount", NodePair.Count);
    MeshRefit.SetBuffer(NodeUpdateKernel, "AllNodes", NodeBuffer);
    MeshRefit.SetBuffer(NodeUpdateKernel, "BVHNodes", BVHDataBuffer);
    MeshRefit.SetBuffer(NodeUpdateKernel, "ToBVHIndex", ToBVHIndexBuffer);
    MeshRefit.Dispatch(NodeUpdateKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);

    MeshRefit.SetInt("NodeCount", BVH.BVH8Nodes.Count);
    MeshRefit.SetBuffer(NodeCompressKernel, "BVHNodes", BVHDataBuffer);
    MeshRefit.SetBuffer(NodeCompressKernel, "AggNodes", BVHBuffer);
    MeshRefit.Dispatch(NodeCompressKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
    }   
}


private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3) {
float a = Vector3.Distance(pt1, pt2);
float b = Vector3.Distance(pt2, pt3);
float c = Vector3.Distance(pt3, pt1);
float s = (a + b + c) / 2.0f;
return Mathf.Sqrt(s * (s-a) * (s-b) * (s-c));
}
private float luminance(float r, float g, float b) {
return 0.299f * r + 0.587f * g + 0.114f * b;
}


public async Task BuildTotal() {
    Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
    Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
    Vector3 V1, V2, V3, Norm1, Norm2, Norm3, Tan1, Tan2, Tan3;
    PrimitiveData TempPrim = new PrimitiveData();
    float TotalEnergy = 0.0f;
for(int i = 0; i < TotalObjects; i++) {

    Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
    Matrix4x4 TransMat = ParentMatInv * ChildMat;
    Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
    Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
        for(int i3 = TransformIndexes[i].VertexStart; i3 < TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount; i3 += 3) {//Transforming child meshes into the space of their parent
            int Index1 = CurMeshData.Indices[i3];
            int Index2 = CurMeshData.Indices[i3 + 2];
            int Index3 = CurMeshData.Indices[i3 + 1];
            V1 = CurMeshData.Verticies[Index1] + Ofst;
            V2 = CurMeshData.Verticies[Index2] + Ofst;
            V3 = CurMeshData.Verticies[Index3] + Ofst;
            V1 = TransMat * V1;
            V2 = TransMat * V2;
            V3 = TransMat * V3;
            TempPrim.V1 = V1 - Ofst2;
            TempPrim.V2 = V2 - Ofst2;
            TempPrim.V3 = V3 - Ofst2;
            Norm1 = ChildMat * CurMeshData.Normals[Index1];
            Norm2 = ChildMat * CurMeshData.Normals[Index2];
            Norm3 = ChildMat * CurMeshData.Normals[Index3];

            Tan1 = ChildMat * CurMeshData.Tangents[Index1];
            Tan2 = ChildMat * CurMeshData.Tangents[Index2];
            Tan3 = ChildMat * CurMeshData.Tangents[Index3];


            TempPrim.Norm1 = ParentMatInv * Norm1;
            TempPrim.Norm2 = ParentMatInv * Norm2;
            TempPrim.Norm3 = ParentMatInv * Norm3;

            TempPrim.Tan1 = ParentMatInv * Tan1;
            TempPrim.Tan2 = ParentMatInv * Tan2;
            TempPrim.Tan3 = ParentMatInv * Tan3;
            
            TempPrim.tex1 = CurMeshData.UVs[Index1];
            TempPrim.tex2 = CurMeshData.UVs[Index2];
            TempPrim.tex3 = CurMeshData.UVs[Index3];

            TempPrim.MatDat = CurMeshData.MatDat[i3 / 3];

            TempPrim.Reconstruct();
            Triangles.Add(TempPrim);
            if(_Materials[TempPrim.MatDat].emmissive > 0.0f) {
                V1 = TempPrim.V1;
                V2 = TempPrim.V2;
                V3 = TempPrim.V3;
                Vector3 Radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor;
                float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                float e = radiance * area;
                TotalEnergy += e;
                TotEnergy += e;

                LightTriangles.Add(new CudaLightTriangle() {
                    pos0 = V1,
                    posedge1 = V2 - V1,
                    posedge2 = V3 - V1,
                    Norm = (TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3) / 3.0f,
                    radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor * area,
                    sumEnergy = TotalEnergy,
                    energy = e
                    });
            }
        }
    
}
LightTriangles.Sort((s1,s2) => s1.energy.CompareTo(s2.energy));
TotalEnergy = 0.0f;
int LightTriCount = LightTriangles.Count;
for(int i = 0; i < LightTriCount; i++) {
    CudaLightTriangle TempTri = LightTriangles[i];
    TotalEnergy += TempTri.energy;
    TempTri.sumEnergy = TotalEnergy;
    LightTriangles[i] = TempTri; 
}
LightCount = LightTriangles.Count;
ConstructAABB();
Construct();
CompileTriangles();
Aggregate();
HasCompleted = true;
Debug.Log(Name + " Has Completed Building with " + AggTriangles.Length + " triangles");
}


public void UpdateData(ref int a, ref int b, ref int ReturnOffset, ref int MaterialOffset) {
        MaterialOffset += MatOffset;
        MatOffset = _Materials.Count;
        b += BVH.cwbvhnode_count;
        a += BVH.cwbvhindex_count;
        AggIndexCount = BVH.cwbvhindex_count;
        AggBVHNodeCount = BVH.cwbvhnode_count;
        ReturnOffset += StaticBVHOffset;
        UpdateAABB();
}

public void CompileTriangles() {
    AggTriangles = new CudaTriangle[Triangles.Count];
    CudaTriangle TempTri = new CudaTriangle();
            int TriCount = Triangles.Count;
            PrimitiveData triangle;
            for(int i2 = 0; i2 < TriCount; ++i2) {//This constructs the list of triangles that actually get sent to the GPU
                triangle = Triangles[BVH.cwbvh_indices[i2]];
                TempTri.pos0 = triangle.V1;

                TempTri.posedge1 = triangle.V2 - triangle.V1;
                TempTri.posedge2 = triangle.V3 - triangle.V1;

                TempTri.norm0 = triangle.Norm1;
                TempTri.normedge1 = triangle.Norm2 - triangle.Norm1;
                TempTri.normedge2 = triangle.Norm3 - triangle.Norm1;

                TempTri.tan0 = triangle.Tan1;
                TempTri.tanedge1 = triangle.Tan2 - triangle.Tan1;
                TempTri.tanedge2 = triangle.Tan3 - triangle.Tan1;

                TempTri.tex0 = triangle.tex1;
                TempTri.texedge1 = triangle.tex2;
                TempTri.texedge2 = triangle.tex3;

                TempTri.MatDat = (uint)triangle.MatDat;
                AggTriangles[i2] = TempTri;
             }
       
}



//Better Bounding Box Transformation by Zuex(I got it from Zuen)
private Vector3 transform_position(Matrix4x4 matrix, Vector3 position) {
    return new Vector3(
        matrix[0, 0] * position.x + matrix[0, 1] * position.y + matrix[0, 2] * position.z + matrix[0, 3],
        matrix[1, 0] * position.x + matrix[1, 1] * position.y + matrix[1, 2] * position.z + matrix[1, 3],
        matrix[2, 0] * position.x + matrix[2, 1] * position.y + matrix[2, 2] * position.z + matrix[2, 3]
    );
}
private Vector3 transform_direction(Matrix4x4 matrix, Vector3 direction) {
    return new Vector3(
        matrix[0, 0] * direction.x + matrix[0, 1] * direction.y + matrix[0, 2] * direction.z,
        matrix[1, 0] * direction.x + matrix[1, 1] * direction.y + matrix[1, 2] * direction.z,
        matrix[2, 0] * direction.x + matrix[2, 1] * direction.y + matrix[2, 2] * direction.z
    );
}
private Matrix4x4 abs(Matrix4x4 matrix) {
    Matrix4x4 result = new Matrix4x4();
    for (int i = 0; i < 4; i++) {
        for (int i2 = 0; i2 < 4; i2++) result[i,i2] = Mathf.Abs(matrix[i,i2]);
    }
    return result;
}

public void UpdateAABB() {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
    Vector3 center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
    Vector3 extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);

    Vector3 new_center = transform_position (this.transform.worldToLocalMatrix.inverse, center);
    Vector3 new_extent = transform_direction(abs(this.transform.worldToLocalMatrix.inverse), extent);

    aabb.BBMin = new_center - new_extent;
    aabb.BBMax = new_center + new_extent;
}

private void ConstructAABB() {
    aabb_untransformed = new AABB();
    aabb_untransformed.init();
    for(int i = 0; i < Triangles.Count; i++) {
        aabb_untransformed.Extend(Triangles[i].BBMax, Triangles[i].BBMin);
    }
}




public void Aggregate() {//Compress the CWBVH
    AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Count];
    BVHNode8DataCompressed TempBVHNode = new BVHNode8DataCompressed();
    for(int i = 0; i < BVH.BVH8Nodes.Count; ++i) {
        BVHNode8Data TempNode = BVH.BVH8Nodes[i];
        uint tempbyte = (TempNode.e[0] | (TempNode.e[1] << 8) | (TempNode.e[2] << 16) | (TempNode.imask << 24));
        uint metafirst = (TempNode.meta[0] | (TempNode.meta[1] << 8) | (TempNode.meta[2] << 16) | (TempNode.meta[3] << 24));
        uint metasecond = (TempNode.meta[4] | (TempNode.meta[5] << 8) | (TempNode.meta[6] << 16) | (TempNode.meta[7] << 24));
        uint minxfirst = (TempNode.quantized_min_x[0] | (TempNode.quantized_min_x[1] << 8) | (TempNode.quantized_min_x[2] << 16) | (TempNode.quantized_min_x[3] << 24));
        uint minxsecond = (TempNode.quantized_min_x[4] | (TempNode.quantized_min_x[5] << 8) | (TempNode.quantized_min_x[6] << 16) | (TempNode.quantized_min_x[7] << 24));
        uint maxxfirst = (TempNode.quantized_max_x[0] | (TempNode.quantized_max_x[1] << 8) | (TempNode.quantized_max_x[2] << 16) | (TempNode.quantized_max_x[3] << 24));
        uint maxxsecond = (TempNode.quantized_max_x[4] | (TempNode.quantized_max_x[5] << 8) | (TempNode.quantized_max_x[6] << 16) | (TempNode.quantized_max_x[7] << 24));
        uint minyfirst = (TempNode.quantized_min_y[0] | (TempNode.quantized_min_y[1] << 8) | (TempNode.quantized_min_y[2] << 16) | (TempNode.quantized_min_y[3] << 24));
        uint minysecond = (TempNode.quantized_min_y[4] | (TempNode.quantized_min_y[5] << 8) | (TempNode.quantized_min_y[6] << 16) | (TempNode.quantized_min_y[7] << 24));
        uint maxyfirst = (TempNode.quantized_max_y[0] | (TempNode.quantized_max_y[1] << 8) | (TempNode.quantized_max_y[2] << 16) | (TempNode.quantized_max_y[3] << 24));
        uint maxysecond = (TempNode.quantized_max_y[4] | (TempNode.quantized_max_y[5] << 8) | (TempNode.quantized_max_y[6] << 16) | (TempNode.quantized_max_y[7] << 24));
        uint minzfirst = (TempNode.quantized_min_z[0] | (TempNode.quantized_min_z[1] << 8) | (TempNode.quantized_min_z[2] << 16) | (TempNode.quantized_min_z[3] << 24));
        uint minzsecond = (TempNode.quantized_min_z[4] | (TempNode.quantized_min_z[5] << 8) | (TempNode.quantized_min_z[6] << 16) | (TempNode.quantized_min_z[7] << 24));
        uint maxzfirst = (TempNode.quantized_max_z[0] | (TempNode.quantized_max_z[1] << 8) | (TempNode.quantized_max_z[2] << 16) | (TempNode.quantized_max_z[3] << 24));
        uint maxzsecond = (TempNode.quantized_max_z[4] | (TempNode.quantized_max_z[5] << 8) | (TempNode.quantized_max_z[6] << 16) | (TempNode.quantized_max_z[7] << 24));

            TempBVHNode.node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z);
            TempBVHNode.node_0w = tempbyte;
            TempBVHNode.node_1x = TempNode.base_index_child;
            TempBVHNode.node_1y = TempNode.base_index_triangle;
            TempBVHNode.node_1z = metafirst;
            TempBVHNode.node_1w = metasecond;
            TempBVHNode.node_2x = minxfirst;
            TempBVHNode.node_2y = minxsecond;
            TempBVHNode.node_2z = maxxfirst;
            TempBVHNode.node_2w = maxxsecond;
            TempBVHNode.node_3x = minyfirst;
            TempBVHNode.node_3y = minysecond;
            TempBVHNode.node_3z = maxyfirst;
            TempBVHNode.node_3w = maxysecond;
            TempBVHNode.node_4x = minzfirst;
            TempBVHNode.node_4y = minzsecond;
            TempBVHNode.node_4z = maxzfirst;
            TempBVHNode.node_4w = maxzsecond;

            AggNodes[i] = TempBVHNode;
    }
    MeshCountChanged = false;
}






private void OnEnable() {
    HasStarted = false;
    if(gameObject.scene.isLoaded) {
        this.GetComponentInParent<AssetManager>().AddQue.Add(this);
        this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
        HasCompleted = false;
    }
}

private void OnDisable() {
    HasStarted = false;
    if(gameObject.scene.isLoaded) {
        this.GetComponentInParent<AssetManager>().RemoveQue.Add(this);
        this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
        HasCompleted = false;
    }
}




}
