using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueList_SO", menuName = "GameData/DialogueList", order = 1)]
public class DialogueList : ScriptableObject
{
    [SerializeField] List<string> dialogues = new List<string>();
    public List<string> GetDialogues => dialogues;
}
