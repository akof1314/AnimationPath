using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimationPath))]
public class AnimationPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AnimationPathSceneUI.OnInspectorGUI((target as MonoBehaviour).gameObject);
    }

    private void OnSceneGUI()
    {
        AnimationPathSceneUI.OnSceneGUI();
    }
}
