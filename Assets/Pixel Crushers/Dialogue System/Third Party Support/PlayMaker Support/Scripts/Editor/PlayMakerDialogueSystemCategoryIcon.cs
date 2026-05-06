using UnityEditor;
using HutongGames.PlayMakerEditor;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [InitializeOnLoad]
    public class PlayMakerDialogueSystemCategoryIcon : MonoBehaviour
    {

        static PlayMakerDialogueSystemCategoryIcon()
        {
            //HutongGames.PlayMakerEditor.CategoryIcon.Register("Dialogue System", CategoryIcon);
        }

        private static Texture s_categoryIcon = null;
        internal static Texture CategoryIcon
        {
            get
            {
                if (s_categoryIcon == null) s_categoryIcon = Resources.Load<Texture>("DialogueSystem");
                if (s_categoryIcon != null) s_categoryIcon.hideFlags = HideFlags.DontSaveInEditor;
                return s_categoryIcon;
            }
        }
    }
}
