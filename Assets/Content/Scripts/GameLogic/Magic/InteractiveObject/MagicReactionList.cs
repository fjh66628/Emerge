using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "MagicReactionList_SO", fileName = "GameData/MagicReactionList", order = 1)]
public class MagicReactionList : ScriptableObject//Inspector里配置的一组反应
{
    [SerializeField] private List<MagicReaction> reactions;
    public List<MagicReaction> GetReactions => reactions;
}
