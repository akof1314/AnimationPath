using System;
using UnityEditor;
using UnityEngine;

public static class AnimationWindowUtil
{
    private static float s_PrevCurrentTime;
    private static Func<float> s_GetCurrentTimeFunc;
    private static Action<float> s_CurrentTimeChange;

    /// <summary>
    /// 注册动画窗口的时间轴时间变化监听
    /// 注意：只监听第一个动画窗口
    /// </summary>
    public static void RegisterTimeChangeListener(Action<float> currentTimeChange)
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return;
        }

        s_GetCurrentTimeFunc = () => { return animationWindowReflect.currentTime; };
        s_PrevCurrentTime = -1f;
        s_CurrentTimeChange = currentTimeChange;
        EditorApplication.update = (EditorApplication.CallbackFunction)
                Delegate.RemoveAll(EditorApplication.update, new EditorApplication.CallbackFunction(OnCurrentTimeListening));
        EditorApplication.update = (EditorApplication.CallbackFunction)
                Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(OnCurrentTimeListening));
    }

    /// <summary>
    /// 取消注册动画窗口的时间轴时间变化监听
    /// </summary>
    public static void UnRegisterTimeChangeListener()
    {
        EditorApplication.update = (EditorApplication.CallbackFunction)
                Delegate.RemoveAll(EditorApplication.update, new EditorApplication.CallbackFunction(OnCurrentTimeListening));
        s_PrevCurrentTime = -1f;
        s_GetCurrentTimeFunc = null;
        s_CurrentTimeChange = null;
    }

    private static void OnCurrentTimeListening()
    {
        float currentTime = -1f;
        if (s_GetCurrentTimeFunc != null)
        {
            currentTime = s_GetCurrentTimeFunc();
        }
        if (!Mathf.Approximately(currentTime, s_PrevCurrentTime))
        {
            s_PrevCurrentTime = currentTime;

            if (s_CurrentTimeChange != null)
            {
                s_CurrentTimeChange(currentTime);
            }
        }
    }

    /// <summary>
    /// 设置动画窗口的当前动画片段项
    /// </summary>
    /// <param name="clipName"></param>
    public static bool SetActiveAnimationClip(string clipName)
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return false;
        }
        GameObject activeRootGameObject = animationWindowReflect.activeRootGameObject;
        if (activeRootGameObject == null)
        {
            Debug.Log("没有动画 activeRootGameObject！");
            return false;
        }

        AnimationClip animationClipSelected = null;
        AnimationClip[] animationClips = AnimationUtility.GetAnimationClips(activeRootGameObject);
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
            return false;
        }

        animationWindowReflect.activeAnimationClip = animationClipSelected;
        return true;
    }

    /// <summary>
    /// 设置动画窗口的当前动画片段项
    /// </summary>
    /// <param name="clipName"></param>
    public static bool SetActiveAnimationClip(AnimationClip clip)
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return false;
        }
        GameObject activeRootGameObject = animationWindowReflect.activeRootGameObject;
        if (activeRootGameObject == null)
        {
            Debug.Log("没有动画 activeRootGameObject！");
            return false;
        }

        animationWindowReflect.activeAnimationClip = clip;
        return true;
    }

    /// <summary>
    /// 获取当前活动的动画片段
    /// </summary>
    /// <returns></returns>
    public static AnimationClip GetActiveAnimationClip()
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return null;
        }
        return animationWindowReflect.activeAnimationClip;
    }

    public static void SetOnClipSelectionChanged(Action onClipSelectionChangedAction, bool removeOnly = false)
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return;
        }

        Action onClipSelectionChanged = animationWindowReflect.onClipSelectionChanged;
        onClipSelectionChanged = (Action)Delegate.RemoveAll(onClipSelectionChanged, onClipSelectionChangedAction);
        if (!removeOnly)
        {
            onClipSelectionChanged = (Action)Delegate.Combine(onClipSelectionChanged, onClipSelectionChangedAction);
        }
        animationWindowReflect.onClipSelectionChanged = onClipSelectionChanged;
    }

    public static void SetCurrentTime(float time)
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return;
        }

        animationWindowReflect.currentTime = time;
#if !UNITY_5_6_OR_NEWER
        animationWindowReflect.recording = true;
        animationWindowReflect.playing = false;
        animationWindowReflect.ResampleAnimation();
#endif
        animationWindowReflect.firstAnimationWindow.Repaint();
    }

    public static float GetCurrentTime()
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return -1f;
        }

        return animationWindowReflect.currentTime;
    }

    public static void Repaint()
    {
        AnimationWindowReflect animationWindowReflect = GetAnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            return;
        }

        animationWindowReflect.firstAnimationWindow.Repaint();
    }

    public static AnimationWindowReflect GetAnimationWindowReflect()
    {
        AnimationWindowReflect animationWindowReflect = new AnimationWindowReflect();
        if (!animationWindowReflect.firstAnimationWindow)
        {
            Debug.Log("没有动画编辑器！");
        }
        return animationWindowReflect;
    }
}
