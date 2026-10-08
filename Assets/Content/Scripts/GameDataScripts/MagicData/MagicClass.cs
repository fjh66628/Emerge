using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MagicClass
{
    [SerializeField] private MagicType magicType;
    public MagicType GetMagicType => magicType;
    [SerializeField] private MagicElement magicElement;
    public MagicElement GetMagicElement => magicElement;
    [SerializeField] private string magicName;
    public string GetMagicName => magicName;
    [SerializeField] private string description;
    public string GetDescription => description;
    [SerializeField] private int damage;
    public int GetDamage => damage;
    [SerializeField] private int manaCost;
    public int GetManaCost => manaCost;
    [SerializeField] private Image magicImage;//魔法UI图片
    public Image GetMagicImage => magicImage;
}
