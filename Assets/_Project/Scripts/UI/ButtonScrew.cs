using UnityEngine;
using UnityEngine.UI;

namespace RobotStrategy.Battle
{
    // UIのねじを画像なしで描きます。
    [AddComponentMenu("Robot Strategy/Mechanical Rivet")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "MechanicalRivet")]
    public sealed class ButtonScrew : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var center = r.center;
            float radius = Mathf.Min(r.width, r.height) * .5f;
            Disc(vh, center, radius, new Color(.035f,.045f,.055f));
            Disc(vh, center + new Vector2(0,.35f), radius*.78f, color);
            Disc(vh, center, radius*.55f, color*.72f);
            float w=radius*1.05f, h=Mathf.Max(.7f,radius*.23f);
            int start=vh.currentVertCount;
            Color slot=new Color(.045f,.055f,.065f,1);
            vh.AddVert(center+new Vector2(-w/2,-h/2),slot,Vector2.zero);
            vh.AddVert(center+new Vector2(-w/2,h/2),slot,Vector2.zero);
            vh.AddVert(center+new Vector2(w/2,h/2),slot,Vector2.zero);
            vh.AddVert(center+new Vector2(w/2,-h/2),slot,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        private static void Disc(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            int start=vh.currentVertCount;
            vh.AddVert(center,tint,Vector2.zero);
            const int segments=16;
            for(int i=0;i<segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);
            }
            for(int i=0;i<segments;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%segments);
        }
    }
}
