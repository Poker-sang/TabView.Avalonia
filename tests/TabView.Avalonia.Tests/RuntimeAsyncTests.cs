using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace TabView.Avalonia.Tests;

public class RuntimeAsyncTests
{
    [Fact]
    public async Task AsyncMethodsUseTheTargetRuntimeAndResumeAfterSuspension()
    {
        var method = typeof(RuntimeAsyncTests).GetMethod(nameof(YieldAndReturn), BindingFlags.NonPublic | BindingFlags.Static)!;
        const MethodImplAttributes runtimeAsync = (MethodImplAttributes)0x2000;
#if NET11_0_OR_GREATER
        Assert.True(method.GetMethodImplementationFlags().HasFlag(runtimeAsync));
#else
        Assert.False(method.GetMethodImplementationFlags().HasFlag(runtimeAsync));
#endif
        Assert.Equal(42, await YieldAndReturn());
    }

    private static async Task<int> YieldAndReturn()
    {
        await Task.Yield();
        return 42;
    }
}
