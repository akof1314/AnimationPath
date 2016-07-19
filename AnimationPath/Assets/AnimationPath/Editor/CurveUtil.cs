using UnityEngine;

public static class CurveUtil
{
    public enum TangentMode
    {
        Editable = 0,
        Smooth = 1,
        Linear = 2,
        Stepped = 3
    }

    public static void SetKeyBroken(ref Keyframe key, bool broken)
    {
        if (broken)
        {
            key.tangentMode |= 1;
        }
        else
        {
            key.tangentMode &= -2;
        }
    }

    public static bool GetKeyBroken(Keyframe key)
    {
        return (key.tangentMode & 1) != 0;
    }

    public static void SetKeyTangentMode(ref Keyframe key, int leftRight, TangentMode mode)
    {
        if (leftRight == 0)
        {
            key.tangentMode &= -7;
            key.tangentMode |= (int)((int)mode << 1);
        }
        else
        {
            key.tangentMode &= -25;
            key.tangentMode |= (int)((int)mode << 3);
        }
        if (GetKeyTangentMode(key, leftRight) != mode)
        {
            Debug.Log("bug");
        }
    }

    public static TangentMode GetKeyTangentMode(Keyframe key, int leftRight)
    {
        if (leftRight == 0)
        {
            return (TangentMode)((key.tangentMode & 6) >> 1);
        }
        return (TangentMode)((key.tangentMode & 24) >> 3);
    }
}
