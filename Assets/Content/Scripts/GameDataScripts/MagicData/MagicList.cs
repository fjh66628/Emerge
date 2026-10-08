using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "MagicList_SO", menuName = "GameData/MagicList", order = 1)]
public class MagicList : ScriptableObject
{
    [SerializeField] List<MagicClassBase> magicList = new List<MagicClassBase>();
    public List<MagicClassBase> GetMagicList => magicList;
}
