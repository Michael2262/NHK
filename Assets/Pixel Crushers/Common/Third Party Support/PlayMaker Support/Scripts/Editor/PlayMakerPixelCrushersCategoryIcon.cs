using UnityEditor;
using HutongGames.PlayMakerEditor;
using UnityEngine;

namespace PixelCrushers.PlayMaker
{

    [InitializeOnLoad]
    public class PlayMakerPixelCrushersCategoryIcon : MonoBehaviour
    {

        static PlayMakerPixelCrushersCategoryIcon()
        {
            //Actions.AddCategoryIcon("Pixel Crushers Common", CategoryIcon);
        }

        private static Texture s_categoryIcon = null;
        internal static Texture CategoryIcon
        {
            get
            {
                if (s_categoryIcon == null) s_categoryIcon = Resources.Load<Texture>("PixelCrushers");
                if (s_categoryIcon != null) s_categoryIcon.hideFlags = HideFlags.DontSaveInEditor;
                return s_categoryIcon;
            }
        }
    }
}
