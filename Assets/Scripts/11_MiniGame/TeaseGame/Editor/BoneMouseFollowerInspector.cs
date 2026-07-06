using UnityEditor;
using UnityEngine;
using Spine;
using Spine.Unity;
using Spine.Unity.Editor;

/// <summary>
/// BoneMouseFollower 的自訂 Inspector。
/// 仿照 Spine 官方 BoneFollowerInspector：選取物件時在 Scene 視窗畫出骨頭位置，
/// 方便挑選與確認目標骨頭。
///   - 尚未填骨頭名稱（或勾選「顯示全部骨頭」）→ 畫出整副骨架 + 骨頭名稱。
///   - 已選定骨頭 → 高亮該骨頭並標示名稱。
/// </summary>
[CustomEditor(typeof(BoneMouseFollower))]
public class BoneMouseFollowerInspector : Editor
{
    // 記在 EditorPrefs，跨物件選取與重啟保留
    private const string ShowAllBonesPrefKey = "BoneMouseFollower.ShowAllBones";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        bool showAll = EditorPrefs.GetBool(ShowAllBonesPrefKey, false);
        bool newShowAll = EditorGUILayout.ToggleLeft(
            "在 Scene 視窗顯示全部骨頭位置", showAll);

        if (newShowAll != showAll)
        {
            EditorPrefs.SetBool(ShowAllBonesPrefKey, newShowAll);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        var follower = (BoneMouseFollower)target;

        SkeletonAnimation skeletonAnimation = follower.skeletonAnimation != null
            ? follower.skeletonAnimation
            : follower.GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null) return;

        skeletonAnimation.Initialize(false); // Edit Mode 下確保 Skeleton 已建立
        Skeleton skeleton = skeletonAnimation.Skeleton;
        if (skeleton == null) return;

        Transform skeletonTransform = skeletonAnimation.transform;
        bool showAll = EditorPrefs.GetBool(ShowAllBonesPrefKey, false);
        bool hasBoneSelected = !string.IsNullOrEmpty(follower.boneName);

        // 沒選骨頭或使用者要求 → 畫出全部骨頭 + 名稱
        if (showAll || !hasBoneSelected)
        {
            SpineHandles.DrawBones(skeletonTransform, skeleton);
            SpineHandles.DrawBoneNames(skeletonTransform, skeleton);
        }

        // 已選定骨頭 → 高亮 + 名稱標籤
        if (hasBoneSelected)
        {
            Bone bone = skeleton.FindBone(follower.boneName);
            if (bone != null)
            {
                SpineHandles.DrawBoneWireframe(skeletonTransform, bone, SpineHandles.TransformContraintColor);
                Handles.Label(bone.GetWorldPosition(skeletonTransform), bone.Data.Name, SpineHandles.BoneNameStyle);
            }
        }
    }
}
