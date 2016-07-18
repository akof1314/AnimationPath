using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor;

[CustomEditor(typeof(AnimationPath))]
public class AnimationPathEditor : Editor
{
    private List<Vector3> posList = new List<Vector3>();
    public override void OnInspectorGUI()
    {
        AnimationPathSceneUI.OnInspectorGUI((target as MonoBehaviour).gameObject);
    }

    private void OnSceneGUI()
    {
        //DrawPos();
        AnimationPathSceneUI.OnSceneGUI();

        if (GUI.changed)
        {
        }
    }

    private void CalcPos()
    {
        GameObject go = (target as MonoBehaviour).gameObject;
        AnimationClip[] clips = AnimationUtility.GetAnimationClips(go);
        if (clips.Length == 0)
        {
            return;
        }

        AnimationClip clip = clips[0];
        //foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        //{
        //    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
        //    Debug.Log(binding.path + "/" + binding.propertyName + " " + binding.type + ", Keys: " + curve.keys.Length);
        //    foreach (var keyframe in curve.keys)
        //    {
        //        Debug.Log(keyframe.time + " " + keyframe.value);
        //    }
        //}

        String inPath = String.Empty;
        Type inType = typeof (Transform);
        AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.x"));
        AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.y"));
        AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.z"));

        posList.Clear();
        foreach (var keyframe in curveX.keys)
        {
            Vector3 pos = new Vector3(
                curveX.Evaluate(keyframe.time),
                curveY.Evaluate(keyframe.time),
                curveZ.Evaluate(keyframe.time)
                );
            posList.Add(pos);

            Debug.Log(keyframe.inTangent + " " + keyframe.outTangent);
        }

        foreach (var pos in posList)
        {
            Debug.Log(pos);
        }
        SceneView.RepaintAll();
        //foreach (var binding in AnimationUtility.GetAnimatableBindings(go, go))
        //{
        //    Debug.Log(binding.path + "/" + binding.propertyName);
        //}
		AnimationUtility.onCurveWasModified = (AnimationUtility.OnCurveWasModified)Delegate.Combine(AnimationUtility.onCurveWasModified, 
            new AnimationUtility.OnCurveWasModified(CurveWasModified));
    }

    private void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType deleted)
    {
        Debug.LogFormat("修改的是 " + clip.name + " bind " + binding.propertyName + " type " + deleted);
    }

    private void DrawPos()
    {
        GameObject go = (target as MonoBehaviour).gameObject;
        AnimationClip[] clips = AnimationUtility.GetAnimationClips(go);
        if (clips.Length == 0)
        {
            return;
        }

        AnimationClip clip = clips[2];
        //foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        //{
        //    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
        //    Debug.Log(binding.path + "/" + binding.propertyName + " " + binding.type + ", Keys: " + curve.keys.Length);
        //    foreach (var keyframe in curve.keys)
        //    {
        //        Debug.Log(keyframe.time + " " + keyframe.value);
        //    }
        //}

        String inPath = String.Empty;
        Type inType = typeof(Transform);
        AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.x"));
        AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.y"));
        AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.z"));

        List<AnimationPathPoint> points = AnimationPathPoint.MakePoints(curveX, curveY, curveZ);

        Handles.color = Color.green;
        int numPos = points.Count;
        for (int i = 0; i < numPos; i++)
        {
            float pointHandleSize = HandleUtility.GetHandleSize(points[i].position) * 0.1f;
            Handles.Label(points[i].position, "  Point " + i);
            //Handles.DotCap(0, points[i].position, Quaternion.identity, pointHandleSize);
            if (Handles.Button(points[i].position, Quaternion.identity, pointHandleSize, pointHandleSize, Handles.DotCap))
            {
                
            }
        }

        for (int i = 0; i < numPos - 1; i++)
        {
            Vector3 startTangent;
            Vector3 endTangent;
            AnimationPathPoint.CalcTangents(points[i], points[i + 1], out startTangent, out endTangent);
            Handles.DrawBezier(points[i].position, points[i + 1].position, startTangent, endTangent,
                           Color.white, null, 2f);
        }
    }
}
