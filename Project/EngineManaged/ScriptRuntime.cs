using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class ScriptRuntime
{
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static void Init() => SnakeGame.Init();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static void Update(float dt) => SnakeGame.Update(dt);
}
