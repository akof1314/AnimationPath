using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AnimationWindowUtil
{
    /// <summary>
    /// 设置动画窗口的当前动画片段项
    /// </summary>
    /// <param name="clipName"></param>
    public static void SetActiveAnimationClip(string clipName)
    {
        Assembly assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
        Type typeAnimationWindow = assembly.GetType("UnityEditor.AnimationWindow");

        FieldInfo animEditorInfo = typeAnimationWindow.GetField("m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getAllAnimationWindowsInfo = typeAnimationWindow.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.Static);
        IList animationWindows = getAllAnimationWindowsInfo.Invoke(null, null) as IList;
        foreach (var animationWindow in animationWindows)
        {
            object animEditor = animEditorInfo.GetValue(animationWindow);
            SetActiveAnimationClipInternal(assembly, animEditor, clipName);
        }
    }

    private static void SetActiveAnimationClipInternal(Assembly assembly, object animEditor, string clipName)
    {
        if (animEditor == null)
        {
            Debug.Log("没有动画编辑器！");
            return;
        }

        Type typeAnimEditor = assembly.GetType("UnityEditor.AnimEditor");
        FieldInfo animationWindowStateInfo = typeAnimEditor.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
        object animationWindowState = animationWindowStateInfo.GetValue(animEditor);
        if (animationWindowState == null)
        {
            Debug.Log("没有动画 AnimationWindowState！");
            return;
        }

        Type typeAnimationWindowState = assembly.GetType("UnityEditorInternal.AnimationWindowState");
        PropertyInfo activeRootGameObjectInfo = typeAnimationWindowState.GetProperty("activeRootGameObject", BindingFlags.Instance | BindingFlags.Public);
        object activeRootGameObject = activeRootGameObjectInfo.GetValue(animationWindowState, null);
        if (activeRootGameObject == null)
        {
            Debug.Log("没有动画 activeRootGameObject！");
            return;
        }

        AnimationClip animationClipSelected = null;
        AnimationClip[] animationClips = AnimationUtility.GetAnimationClips(activeRootGameObject as GameObject);
        for (int i = 0; i < animationClips.Length; i++)
        {
            if (animationClips[i].name == clipName)
            {
                animationClipSelected = animationClips[i];
                break;
            }
        }
        if (animationClipSelected == null)
        {
            Debug.Log("没有动画 " + clipName);
            return;
        }

        PropertyInfo activeAnimationClipInfo = typeAnimationWindowState.GetProperty("activeAnimationClip", BindingFlags.Instance | BindingFlags.Public);
        activeAnimationClipInfo.SetValue(animationWindowState, animationClipSelected, null);
    }

    private static float s_PrevCurrentTime;
    private static Func<float> s_getCurrentTime;
    private static Action<float> s_CurrentTimeChange;

    /// <summary>
    /// 注册动画窗口的时间轴时间变化监听
    /// 注意：只监听第一个动画窗口
    /// </summary>
    public static void RegisterTimeChangeListener(Action<float> currentTimeChange)
    {
        Assembly assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
        Type typeAnimationWindow = assembly.GetType("UnityEditor.AnimationWindow");
        FieldInfo animEditorInfo = typeAnimationWindow.GetField("m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getAllAnimationWindowsInfo = typeAnimationWindow.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.Static);
        IList animationWindows = getAllAnimationWindowsInfo.Invoke(null, null) as IList;

        object animEditor = null;
        foreach (var animationWindow in animationWindows)
        {
            animEditor = animEditorInfo.GetValue(animationWindow);
            break;
        }

        if (animEditor == null)
        {
            Debug.Log("没有动画编辑器！");
            return;
        }

        Type typeAnimEditor = assembly.GetType("UnityEditor.AnimEditor");
        FieldInfo animationWindowStateInfo = typeAnimEditor.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
        object animationWindowState = animationWindowStateInfo.GetValue(animEditor);
        if (animationWindowState == null)
        {
            Debug.Log("没有动画 AnimationWindowState！");
            return;
        }

        Type typeAnimationWindowState = assembly.GetType("UnityEditorInternal.AnimationWindowState");
        PropertyInfo currentTimeInfo = typeAnimationWindowState.GetProperty("currentTime", BindingFlags.Instance | BindingFlags.Public);
        s_getCurrentTime = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), animationWindowState, currentTimeInfo.GetGetMethod());

        s_PrevCurrentTime = -1f;
        s_CurrentTimeChange = currentTimeChange;
        EditorApplication.update -= OnCurrentTimeListening;
        EditorApplication.update += OnCurrentTimeListening;
    }

    /// <summary>
    /// 取消注册动画窗口的时间轴时间变化监听
    /// </summary>
    public static void UnRegisterTimeChangeListener()
    {
        EditorApplication.update -= OnCurrentTimeListening;
        s_PrevCurrentTime = -1f;
        s_getCurrentTime = null;
        s_CurrentTimeChange = null;
    }

    private static void OnCurrentTimeListening()
    {
        float currentTime = GetCurrentTime();
        if (!Mathf.Approximately(currentTime, s_PrevCurrentTime))
        {
            s_PrevCurrentTime = currentTime;

            if (s_CurrentTimeChange != null)
            {
                s_CurrentTimeChange(currentTime);
            }
        }
    }

    public static float GetCurrentTime()
    {
        if (s_getCurrentTime == null)
        {
            return -1f;
        }
        return s_getCurrentTime();
    }

    private static Action s_ClipSelectionChanged;

    public static AnimationClip GetActiveAnimationClip(Action onClipSelectionChangedAction)
    {
        s_ClipSelectionChanged = (Action)Delegate.RemoveAll(s_ClipSelectionChanged, onClipSelectionChangedAction);
        Assembly assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
        Type typeAnimationWindow = assembly.GetType("UnityEditor.AnimationWindow");
        FieldInfo animEditorInfo = typeAnimationWindow.GetField("m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getAllAnimationWindowsInfo = typeAnimationWindow.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.Static);
        IList animationWindows = getAllAnimationWindowsInfo.Invoke(null, null) as IList;

        object animEditor = null;
        foreach (var animationWindow in animationWindows)
        {
            animEditor = animEditorInfo.GetValue(animationWindow);
            break;
        }

        if (animEditor == null)
        {
            Debug.Log("没有动画编辑器！");
            return null;
        }

        Type typeAnimEditor = assembly.GetType("UnityEditor.AnimEditor");
        FieldInfo animationWindowStateInfo = typeAnimEditor.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
        object animationWindowState = animationWindowStateInfo.GetValue(animEditor);
        if (animationWindowState == null)
        {
            Debug.Log("没有动画 AnimationWindowState！");
            return null;
        }

        Type typeAnimationWindowState = assembly.GetType("UnityEditorInternal.AnimationWindowState");
        FieldInfo onClipSelectionChangedInfo = typeAnimationWindowState.GetField("onClipSelectionChanged", BindingFlags.Instance | BindingFlags.Public);
        Action onClipSelectionChanged = onClipSelectionChangedInfo.GetValue(animationWindowState) as Action;
        s_ClipSelectionChanged = (Action)Delegate.Combine(s_ClipSelectionChanged, onClipSelectionChangedAction);
        onClipSelectionChanged = (Action)Delegate.Combine(onClipSelectionChanged, s_ClipSelectionChanged);
        onClipSelectionChangedInfo.SetValue(animationWindowState, onClipSelectionChanged);

        PropertyInfo activeAnimationClipInfo = typeAnimationWindowState.GetProperty("activeAnimationClip", BindingFlags.Instance | BindingFlags.Public);
        return activeAnimationClipInfo.GetValue(animationWindowState, null) as AnimationClip;
    }

    public static void SetCurrentTime(float time)
    {
        Assembly assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
        Type typeAnimationWindow = assembly.GetType("UnityEditor.AnimationWindow");
        FieldInfo animEditorInfo = typeAnimationWindow.GetField("m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getAllAnimationWindowsInfo = typeAnimationWindow.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.Static);
        IList animationWindows = getAllAnimationWindowsInfo.Invoke(null, null) as IList;

        if (animationWindows.Count == 0)
        {
            return;
        }
        EditorWindow animationWindow = animationWindows[0] as EditorWindow;
        object animEditor = animEditorInfo.GetValue(animationWindow);
        if (animEditor == null)
        {
            Debug.Log("没有动画编辑器！");
            return;
        }

        Type typeAnimEditor = assembly.GetType("UnityEditor.AnimEditor");
        FieldInfo animationWindowStateInfo = typeAnimEditor.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
        object animationWindowState = animationWindowStateInfo.GetValue(animEditor);
        if (animationWindowState == null)
        {
            Debug.Log("没有动画 AnimationWindowState！");
            return;
        }

        Type typeAnimationWindowState = assembly.GetType("UnityEditorInternal.AnimationWindowState");
        PropertyInfo recordingInfo = typeAnimationWindowState.GetProperty("recording", BindingFlags.Instance | BindingFlags.Public);
        recordingInfo.SetValue(animationWindowState, true, null);

        PropertyInfo currentTimeInfo = typeAnimationWindowState.GetProperty("currentTime", BindingFlags.Instance | BindingFlags.Public);
        currentTimeInfo.SetValue(animationWindowState, time, null);
        animationWindow.Repaint();
    }

    private static void OnClipSelectionChange()
    {
        Debug.Log(1);
    }

    private class AnimationWindowReflect
    {
        private Assembly m_Assembly;

        public Assembly assembly
        {
            get
            {
                if (m_Assembly == null)
                {
                    m_Assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
                }
                return m_Assembly;
            }
        }


    }
}