using System;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using TMPro;

public class StatusPointManager : MonoBehaviour
{
    public int point = 15,
            MaxHP = 0,
            Attack = 0,
            Defence = 0,
            Speed = 0;
    public int sea = 0;    // 0:C 1:B 2:A
    public int ground = 0;
    public int sky = 0;
    public int Humanoid, tank, Battleship, Fighterjet, Transformer,type;


    public TextMeshProUGUI pointText;
    public TextMeshProUGUI MaxHPText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenceText;
    public TextMeshProUGUI speedText; 
    public TextMeshProUGUI groundText;
    public TextMeshProUGUI skyText;
    public TextMeshProUGUI seaText;

   
    public TextMeshProUGUI typeText;


    void Start()
    {
        UpdateText();
    }

    public enum types
    {
        Humanoid,tank ,battleship,fiterjet,transformer
    }

    public void ADDtypes()
    {
        type++;

        if (type > 4)
        {
            type = 0;
        }

        UpdateText();
    }

    public void Removetypes()
    {
        type--;

        if (type < 0)
        {
            type = 4;
        }

        UpdateText();
    }


    //場所の適性です。これはC,B,Aで表示されています。

    public enum Rank
    {
        A, B, C
    }

    public void ADDsea()
    {
        if (sea == 0)
        {
            if (point >= 3)
            {
                sea = 1;
                point -= 3;

            }
        }
        else if (sea == 1)
        {
            if (point >= 3)
            {
                sea = 2;
                point -= 3;

            }
        }
        else if (sea == 2)
        {
            sea = 0;
            point += 6;
        }
        UpdateText();

    }

    public void ADDground()
    {
        if (ground == 0)
        {
            if (point >= 3)
            {
                ground = 1;
                point -= 3;

            }
        }
        else if (ground == 1)
        {
            if (point >= 3)
            {
                ground = 2;
                point -= 3;

            }
        }
        else if (ground == 2)
        {
            ground = 0;
            point += 6;
        }

            UpdateText();
    }


    public void ADDsky()
    {
        if (sky == 0)
        {
            if (point >= 3)
            {
                sky = 1;
                point -= 3;

            }
        }
        else if (sky == 1)
        {
            if (point >= 3)
            {
                sky = 2;
                point -= 3;

            }
        }
        else if (sky == 2)
        {
            sky = 0;
            point += 6;
        }

        UpdateText();
    }


    //ここからステータスの内容です。
    //ADDは上昇、Removeは数値が下がります。

    public void ADDMaxHP()
    {
        if(point > 0)
        {
            MaxHP++;
            point--;
            UpdateText();
        }
    }

    public void RemoveMaxHP()
    {
        if (MaxHP > 0)
        {
            MaxHP--;
            point++;
            UpdateText();
        }
    }

    public void ADDAttack()
    {
        if (point > 0)
        {
            Attack++;
            point--;
            UpdateText();
        }
    }

    public void RemovAttack()
    {
        if (Attack > 0)
        {
            Attack--;
            point++;
            UpdateText();
        }
    }
    public void ADDDefence()
    {
        if (point > 0)
        {
            Defence++;
            point--;
            UpdateText();
        }
    }

    public void RemoveDefence()
    {
        if (Defence > 0)
        {
            Defence--;
            point++;
            UpdateText();
        }
    }


    public void ADDSpeed()
    {
        if (point > 0)
        {
            Speed++;
            point--;
            UpdateText();
        }
    }

    public void RemoveSpeed()
    {
        if (Speed > 0)
        {
            Speed--;
            point++;
            UpdateText();
        }
    }




    void UpdateText()
    {
        pointText.text=point.ToString();
        MaxHPText.text=MaxHP.ToString();
        attackText.text=Attack.ToString();
        defenceText.text=Defence.ToString();
        speedText.text=Speed.ToString();
        groundText.text = ground.ToString();
        skyText.text = sky.ToString();
        seaText.text = sea.ToString();
    
        typeText.text=type.ToString();

        switch (type) {
            case 0:
               typeText.text = "Humanoid";
                break;
            case 1:
               typeText.text = "Battleship";
                break;
            case 2:
               typeText.text = "Fiter jet";
                break;
            case 3:
               typeText.text = "Transform";
                break;
            case 4:
               typeText.text = "Tank";
                break;
        }



        switch (ground)
        {
            case 0:
                groundText.text = "ground:C";
                break;
            case 1:
                groundText.text = "ground:B";
                break;
            case 2:
                groundText.text = "ground:A";
                break;


        }

        switch (sea)
        {
            case 0:
                seaText.text = "sea:C";
                break;
            case 1:
                seaText.text = "sea:B";
                break;
            case 2:
                seaText.text = "sea:A";
                break;
        }

        switch (sky)
        {
            case 0:
                skyText.text = "sky:C";
                break;
            case 1:
                skyText.text = "sky:B";
                break;
            case 2:
                skyText.text = "sky:A";
                break;
        }
    }

}
