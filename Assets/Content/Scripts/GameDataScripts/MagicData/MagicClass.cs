using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MagicClass
{
    public MagicType magicType { get; private set; }
    public MagicElement magicElement { get; private set; }
    public string magicName { get; private set; }
    public string description { get; private set; }
    public int damage { get; private set; }
    public int manaCost { get; private set; }
    public Image magicImage { get; private set; }//魔法UI图片
}
