#nullable enable
using Fake.Helpers;

namespace Fake.Core.Tests.Helpers;

public class ReflectionHelperTests
{
    [Fact]
    public void CreateInstance_应创建无参构造实例()
    {
        var instance = ReflectionHelper.CreateInstance(typeof(DefaultCtorSample));

        instance.ShouldBeOfType<DefaultCtorSample>();
    }

    [Fact]
    public void CreateInstance_应匹配接口参数构造函数()
    {
        var dependency = new DerivedDependency();

        var instance = ReflectionHelper.CreateInstance(typeof(InterfaceCtorSample), dependency);

        instance.ShouldBeOfType<InterfaceCtorSample>()
            .Dependency.ShouldBeSameAs(dependency);
    }

    [Fact]
    public void CreateInstance_应支持null参数匹配引用类型构造函数()
    {
        var instance = ReflectionHelper.CreateInstance(typeof(NullableArgCtorSample), [null!]);

        instance.ShouldBeOfType<NullableArgCtorSample>()
            .Value.ShouldBeNull();
    }

    [Fact]
    public void CreateInstance_应创建值类型默认值()
    {
        var instance = ReflectionHelper.CreateInstance(typeof(ValueTypeSample));

        instance.ShouldBeOfType<ValueTypeSample>()
            .Number.ShouldBe(0);
    }

    [Fact]
    public void CreateInstance_应支持值类型的显式公共无参构造函数()
    {
        var instance = ReflectionHelper.CreateInstance(typeof(ValueTypeWithCtorSample));

        instance.ShouldBeOfType<ValueTypeWithCtorSample>()
            .Number.ShouldBe(7);
    }

    [Fact]
    public void CreateInstance_找不到匹配构造函数时应抛出异常()
    {
        var exception = Should.Throw<FakeException>(() =>
            ReflectionHelper.CreateInstance(typeof(StringCtorSample), new object()));

        exception.Message.ShouldContain("无法为");
        exception.Message.ShouldContain(nameof(StringCtorSample));
    }

    [Fact]
    public void CreateInstance_应优先匹配最精确构造函数()
    {
        var dependency = new DerivedDependency();

        var instance = ReflectionHelper.CreateInstance(typeof(OverloadCtorSample), dependency);

        instance.ShouldBeOfType<OverloadCtorSample>().CtorKind.ShouldBe("exact");
    }

    private sealed class DefaultCtorSample
    {
    }

    private sealed class InterfaceCtorSample(IDependency dependency)
    {
        public IDependency Dependency { get; } = dependency;
    }

    private sealed class NullableArgCtorSample(string? value)
    {
        public string? Value { get; } = value;
    }

    private readonly record struct ValueTypeSample(int Number);

    private struct ValueTypeWithCtorSample
    {
        public ValueTypeWithCtorSample()
        {
            Number = 7;
        }

        public int Number { get; }
    }

    private sealed class StringCtorSample(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class OverloadCtorSample
    {
        public OverloadCtorSample(IDependency dependency)
        {
            Dependency = dependency;
            CtorKind = "assignable";
        }

        public OverloadCtorSample(DerivedDependency dependency)
        {
            Dependency = dependency;
            CtorKind = "exact";
        }

        public IDependency Dependency { get; }

        public string CtorKind { get; }
    }

    private interface IDependency
    {
    }

    private sealed class DerivedDependency : IDependency
    {
    }
}
