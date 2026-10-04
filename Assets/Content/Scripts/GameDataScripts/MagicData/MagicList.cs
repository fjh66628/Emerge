using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "MagicList_SO", menuName = "GameData/MagicList", order = 1)]
public class MagicList : ScriptableObject
{
    [SerializeField] List<MagicClass> magicList = new List<MagicClass>();
    public List<MagicClass> GetMagicList => magicList;
}
