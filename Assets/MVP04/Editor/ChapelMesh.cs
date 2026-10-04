using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MVP04.Editor
{
    // Small batches keep shadow and additional-light culling local to each architectural bay.
    public sealed class ChapelMesh
    {
        readonly List<Vector3> vertices=new List<Vector3>(), normals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<Vector2> uvs=new List<Vector2>();
        readonly List<int> indices=new List<int>();

        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Color color)
        {
            int k=vertices.Count; vertices.AddRange(new[]{a,b,c,d});
            for(int i=0;i<4;i++){normals.Add(normal);colors.Add(color);}
            uvs.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            indices.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
        }

        public void Box(Vector3 centre,Vector3 size,float bevel=.025f,Color? tint=null,Quaternion? rotation=null)
        {
            Color color=tint??Color.white;
            Quaternion q=rotation??Quaternion.identity;
            Vector3 half=size*.5f; bevel=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);
            Vector3 inner=half-Vector3.one*bevel;
            foreach(Vector3 n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n),v=Vector3.Cross(n,u);
                float hu=Vector3.Dot(Abs(u),half),hv=Vector3.Dot(Abs(v),half),hn=Vector3.Dot(Abs(n),half);
                float[] us={-hu,-hu+bevel,hu-bevel,hu},vs={-hv,-hv+bevel,hv-bevel,hv};
                int offset=vertices.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    Vector3 p=n*hn+u*us[x]+v*vs[y];
                    Vector3 clamped=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                    // With no bevel p == clamped. A zero normal produces invalid PBR lighting
                    // which then spreads across the screen through HDR bloom and depth of field.
                    Vector3 normal=bevel>0 ? (p-clamped).normalized : n;
                    vertices.Add(centre+q*(clamped+normal*bevel));normals.Add(q*normal);colors.Add(color);uvs.Add(new Vector2(us[x],vs[y]));
                }
                for(int y=0;y<3;y++)for(int x=0;x<3;x++){int a=offset+y*4+x;indices.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
            }
        }

        public void Cylinder(Vector3 centre,float radius,float height,Color? tint=null,int sides=16)
        {
            Color color=tint??Color.white;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),n=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                Vector3 low=centre-Vector3.up*height*.5f,high=centre+Vector3.up*height*.5f;
                Quad(low+p,high+p,high+n,low+n,(p+n).normalized,color);
                Quad(high,high+n,high+p,high,Vector3.up,color);
                Quad(low,low+p,low+n,low,Vector3.down,color);
            }
        }

        public void Bar(Vector3 start,Vector3 end,float width,Color? color=null)
        {
            Vector3 delta=end-start;
            Box((start+end)*.5f,new Vector3(width,delta.magnitude+width*.2f,width),width*.22f,color,Quaternion.FromToRotation(Vector3.up,delta.normalized));
        }

        public void Arch(Vector3 centre,float radiusX,float radiusY,float thickness,float depth,int segments=32,Quaternion? rotation=null,float span=180)
        {
            Quaternion q=rotation??Quaternion.identity;
            for(int i=0;i<segments;i++)
            {
                float a=(i+.025f)*span*Mathf.Deg2Rad/segments,b=(i+.975f)*span*Mathf.Deg2Rad/segments;
                Vector3 ai=new Vector3(Mathf.Cos(a)*radiusX,Mathf.Sin(a)*radiusY,0),bi=new Vector3(Mathf.Cos(b)*radiusX,Mathf.Sin(b)*radiusY,0);
                Vector3 ao=new Vector3(Mathf.Cos(a)*(radiusX+thickness),Mathf.Sin(a)*(radiusY+thickness),0),bo=new Vector3(Mathf.Cos(b)*(radiusX+thickness),Mathf.Sin(b)*(radiusY+thickness),0);
                Vector3 front=Vector3.back*depth*.5f,back=-front;
                void Face(Vector3 v0,Vector3 v1,Vector3 v2,Vector3 v3,Vector3 n)=>Quad(centre+q*v0,centre+q*v1,centre+q*v2,centre+q*v3,q*n,Color.white);
                Face(ai+front,bi+front,bo+front,ao+front,Vector3.back);
                Face(ao+back,bo+back,bi+back,ai+back,Vector3.forward);
                Face(ai+back,bi+back,bi+front,ai+front,-(ai+bi).normalized);
                Face(ao+front,bo+front,bo+back,ao+back,(ao+bo).normalized);
                Face(ai+front,ao+front,ao+back,ai+back,Vector3.Cross(Vector3.forward,ai).normalized);
                Face(bi+back,bo+back,bo+front,bi+front,Vector3.Cross(bi,Vector3.forward).normalized);
            }
        }

        public void Disc(Vector3 centre,float radius,int segments=96)
        {
            int offset=vertices.Count;
            vertices.Add(centre);normals.Add(Vector3.back);colors.Add(Color.white);uvs.Add(Vector2.one*.5f);
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;Vector2 v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                vertices.Add(centre+new Vector3(v.x,v.y,0)*radius);normals.Add(Vector3.back);colors.Add(Color.white);uvs.Add(v*.5f+Vector2.one*.5f);
                if(i<segments)indices.AddRange(new[]{offset,offset+i+2,offset+i+1});
            }
        }

        public GameObject Save(string name,Material material,bool collision=false)
        {
            string path="Assets/MVP04/Meshes/"+name+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uvs);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
            return go;
        }
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
    }
}
