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
#pragma warning disable CS0618 // 类型或成员已过时
            key.tangentMode |= 1;
#pragma warning restore CS0618 // 类型或成员已过时
        }
        else
        {
#pragma warning disable CS0618 // 类型或成员已过时
            key.tangentMode &= -2;
#pragma warning restore CS0618 // 类型或成员已过时
        }
    }

    public static bool GetKeyBroken(Keyframe key)
    {
#pragma warning disable CS0618 // 类型或成员已过时
        return (key.tangentMode & 1) != 0;
#pragma warning restore CS0618 // 类型或成员已过时
    }

    public static void SetKeyTangentMode(ref Keyframe key, int leftRight, TangentMode mode)
    {
        if (leftRight == 0)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            key.tangentMode &= -7;
#pragma warning restore CS0618 // 类型或成员已过时
#pragma warning disable CS0618 // 类型或成员已过时
            key.tangentMode |= (int)((int)mode << 1);
#pragma warning restore CS0618 // 类型或成员已过时
        }
        else
        {
#pragma warning disable CS0618 // 类型或成员已过时
            key.tangentMode &= -25;
#pragma warning restore CS0618 // 类型或成员已过时

#pragma warning disable CS0618 // 类型或成员已过时
            key.tangentMode |= (int)((int)mode << 3);
#pragma warning restore CS0618 // 类型或成员已过时
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
#pragma warning disable CS0618 // 类型或成员已过时
            return (TangentMode)((key.tangentMode & 6) >> 1);
#pragma warning restore CS0618 // 类型或成员已过时
        }
#pragma warning disable CS0618 // 类型或成员已过时
        return (TangentMode)((key.tangentMode & 24) >> 3);
#pragma warning restore CS0618 // 类型或成员已过时
    }
}
