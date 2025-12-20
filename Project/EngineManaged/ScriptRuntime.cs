using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class ScriptRuntime
{
	[UnmanagedCallersOnly(EntryPoint = "ScriptRuntime_Init", CallConvs = new[] { typeof(CallConvCdecl) })]
	public static void Init()
	{
		Console.WriteLine("ScriptRuntime_Init called");

		try
		{
			Native.Engine_Log("hello from C#");
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

	}

	[UnmanagedCallersOnly(EntryPoint = "ScriptRuntime_Update", CallConvs = new[] { typeof(CallConvCdecl) })]
	public static void Update(float dt)
	{
	
	}
}