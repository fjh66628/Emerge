using UnityEngine;

namespace MVP04
{
    // A passive overlay: it never takes keyboard focus away from the character.
    public sealed class ChapelControlsHint : MonoBehaviour
    {
        private static readonly GUIContent Hint = new GUIContent("WASD / ARROWS   Move     Q / E   Zoom     WHEEL   Zoom     SPACE   Cast");
        private GUIStyle label;

        private void OnGUI()
        {
            if (label == null)
                label = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColour = GUI.color;
            float scale = Mathf.Clamp(Screen.height / 720f, .75f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Mathf.Min(600f, Screen.width / scale - 24f);
            Rect bounds = new Rect((Screen.width / scale - width) * .5f, Screen.height / scale - 46f, width, 30f);
            GUI.color = new Color(.025f, .035f, .055f, .78f);
            GUI.DrawTexture(bounds, Texture2D.whiteTexture);
            GUI.color = new Color(.84f, .86f, .90f);
            GUI.Label(bounds, Hint, label);
            GUI.color = previousColour;
            GUI.matrix = previousMatrix;
        }
    }
}
