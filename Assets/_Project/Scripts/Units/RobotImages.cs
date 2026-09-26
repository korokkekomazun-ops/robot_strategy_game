using UnityEngine;

namespace RobotStrategy.Battle
{
    // 専用画像がない機体へ仮の形を作ります。
    public partial class BattleManager
    {
        // Simple editable shapes are used only until a chassis sprite is assigned.
        private void BuildChassisPlaceholder(BattleUnit unit, ChassisKind kind)
        {
            var art=unit.transform.Find("Artwork");
            art.GetComponent<SpriteRenderer>().enabled=false;
            art.localScale=Vector3.one*(kind==ChassisKind.Giant?2.4f:1.2f);
            Color steel=new Color(.56f,.66f,.70f), dark=new Color(.16f,.22f,.26f), light=new Color(.22f,.90f,1);
            if(kind==ChassisKind.Tank)
            {
                ChassisPart(art,"Left track",-.32f,0,.20f,.85f,dark);
                ChassisPart(art,"Right track",.32f,0,.20f,.85f,dark);
                ChassisPart(art,"Armour",0,0,.52f,.64f,steel);
                ChassisPart(art,"Turret",0,.05f,.30f,.30f,dark);
                ChassisPart(art,"Barrel",0,.34f,.08f,.48f,steel);
            }
            else if(kind==ChassisKind.Ship)
            {
                ChassisPart(art,"Hull",0,-.04f,.40f,.86f,steel);
                ChassisPart(art,"Bow",0,.38f,.28f,.28f,steel,45);
                ChassisPart(art,"Deck",0,0,.25f,.52f,dark);
                ChassisPart(art,"Bridge",0,-.1f,.20f,.20f,light);
                ChassisPart(art,"Gun",0,.27f,.07f,.28f,steel);
            }
            else
            {
                ChassisPart(art,"Left leg",-.17f,-.32f,.18f,.35f,dark);
                ChassisPart(art,"Right leg",.17f,-.32f,.18f,.35f,dark);
                ChassisPart(art,"Torso",0,.02f,.45f,.42f,steel);
                ChassisPart(art,"Left arm",-.34f,0,.17f,.40f,steel);
                ChassisPart(art,"Right arm",.34f,0,.17f,.40f,steel);
                ChassisPart(art,"Head",0,.36f,.27f,.23f,steel);
                ChassisPart(art,"Visor",0,.38f,.20f,.055f,light);
                ChassisPart(art,"Core",0,.06f,.12f,.12f,kind==ChassisKind.Giant?new Color(1,.55f,.15f):light);
                if(kind==ChassisKind.Transformer)
                {
                    ChassisPart(art,"Left wing",-.43f,.16f,.37f,.14f,dark,-25);
                    ChassisPart(art,"Right wing",.43f,.16f,.37f,.14f,dark,25);
                }
            }
        }

        private void ChassisPart(Transform parent,string name,float x,float y,float width,float height,Color color,float angle=0)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(parent,false);
            obj.transform.localPosition=new Vector3(x,y,0);
            obj.transform.localScale=new Vector3(width,height,1);
            obj.transform.localRotation=Quaternion.Euler(0,0,angle);
            var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=whiteSprite;renderer.color=color;
            renderer.sortingOrder=11+parent.childCount;
        }
    }
}
