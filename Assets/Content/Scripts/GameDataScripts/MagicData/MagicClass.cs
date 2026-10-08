using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class MagicClassBase
{
    [SerializeField] protected MagicType magicType;
    public MagicType GetMagicType => magicType;
    //[SerializeField] private MagicElement magicElement;
    //public MagicElement GetMagicElement => magicElement;
    [SerializeField] protected string magicName;
    public string GetMagicName => magicName;
    [SerializeField] protected string description;
    public string GetDescription => description;
    [SerializeField] protected int damage;
    public int GetDamage => damage;
    [SerializeField] protected int manaCost;
    public int GetManaCost => manaCost;
    [SerializeField] protected Image magicImage;//魔法UI图片
    public Image GetMagicImage => magicImage;


    virtual public void OnHitSometing() { }//命中时候触发某些逻辑
}

public class WaterMagic : MagicClassBase
{
    public WaterMagic(MagicList magicList)
    {
        foreach (var item in magicList.GetMagicList)
        {
            if (item.GetMagicType == MagicType.水)
            {
                magicType = item.GetMagicType;
                magicName = item.GetMagicName;
                description = item.GetDescription;
                damage = item.GetDamage;
                manaCost = item.GetManaCost;
                magicImage = item.GetMagicImage;
            }
        }
    }
}

public class FireMagic : MagicClassBase
{
    public FireMagic(MagicList magicList)
    {
        foreach (var item in magicList.GetMagicList)
        {
            if (item.GetMagicType == MagicType.火)
            {
                magicType = item.GetMagicType;
                magicName = item.GetMagicName;
                description = item.GetDescription;
                damage = item.GetDamage;
                manaCost = item.GetManaCost;
                magicImage = item.GetMagicImage;
            }
        }
    }
}

public class RockMagic : MagicClassBase
{
    public RockMagic(MagicList magicList)
    {
        foreach (var item in magicList.GetMagicList)
        {
            if (item.GetMagicType == MagicType.岩)
            {
                magicType = item.GetMagicType;
                magicName = item.GetMagicName;
                description = item.GetDescription;
                damage = item.GetDamage;
                manaCost = item.GetManaCost;
                magicImage = item.GetMagicImage;
            }
        }
    }
}

public class AirMagic : MagicClassBase
{
    public AirMagic(MagicList magicList)
    {
        foreach (var item in magicList.GetMagicList)
        {
            if (item.GetMagicType == MagicType.气)
            {
                magicType = item.GetMagicType;
                magicName = item.GetMagicName;
                description = item.GetDescription;
                damage = item.GetDamage;
                manaCost = item.GetManaCost;
                magicImage = item.GetMagicImage;
            }
        }
    }
}

public class CookMagic : MagicClassBase
{
    public CookMagic(MagicList magicList)
    {
        foreach (var item in magicList.GetMagicList)
        {
            if (item.GetMagicType == MagicType.烤)
            {
                magicType = item.GetMagicType;
                magicName = item.GetMagicName;
                description = item.GetDescription;
                damage = item.GetDamage;
                manaCost = item.GetManaCost;
                magicImage = item.GetMagicImage;
            }
        }
    }
}

public class MeltMagic : MagicClassBase
{
    public MeltMagic(MagicList magicList)
    {
        foreach (var item in magicList.GetMagicList)
        {
            if (item.GetMagicType == MagicType.融)
            {
                magicType = item.GetMagicType;
                magicName = item.GetMagicName;
                description = item.GetDescription;
                damage = item.GetDamage;
                manaCost = item.GetManaCost;
                magicImage = item.GetMagicImage;
            }
        }
    }
}
