// net48 兼容层：补齐 C# 9+ record/init 与 C# 11 required 所需的编译器标记类型，
// 以及 .NET Framework 缺失的 Math.Clamp。这些类型仅供编译器识别，不参与运行时逻辑。

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>record 类型与 init 访问器所需的编译器标记类型</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }

    /// <summary>required 成员检查所需的特性</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }

    /// <summary>标记某成员依赖特定编译器特性（如 required members）</summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public bool IsOptional { get; init; }

        public const string RefStructs = nameof(RefStructs);

        public const string RequiredMembers = nameof(RequiredMembers);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>标记构造函数已设置所有 required 成员</summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
    }
}

namespace System.Collections.Generic
{
    /// <summary>.NET Framework 缺少 KeyValuePair 解构支持，补齐以便 foreach 直接解构</summary>
    internal static class KeyValuePairDeconstructExtensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}

namespace TaskbarCalendar.Compat
{
    /// <summary>.NET Framework 缺少 Math.Clamp，提供等价实现</summary>
    internal static class MathCompat
    {
        /// <summary>将数值限制在 [min, max] 区间内</summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
