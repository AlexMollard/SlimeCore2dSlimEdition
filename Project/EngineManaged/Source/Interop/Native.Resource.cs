using System;
using System.Runtime.InteropServices;

internal static partial class NativeMethods
{
    private const string LibraryName = "SlimeCore2D.exe";
    private const CallingConvention CallConvention = CallingConvention.Cdecl;

    // -------------------------------------------------------------------------
    // TEXTURES
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads a texture via the ResourceManager. 
    /// If 'path' is null/empty, it assumes 'name' is the relative path.
    /// Returns a pointer to the Texture object (IntPtr).
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallConvention)]
    internal static extern IntPtr Resources_LoadTexture([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    /// <summary>
    /// Retrieves an already loaded texture by name. Returns IntPtr.Zero if not found.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallConvention)]
    internal static extern IntPtr Resources_GetTexture([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    // -------------------------------------------------------------------------
    // FONTS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads a font via the ResourceManager (SDF generation).
    /// Default fontSize is usually 48 if you want high quality scaling.
    /// Returns a pointer to the Text object (IntPtr).
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallConvention)]
    internal static extern IntPtr Resources_LoadFont([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int fontSize);

    // -------------------------------------------------------------------------
    // TEXT DATA
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads a text file via the ResourceManager.
    /// Returns a pointer to a null-terminated UTF-8 string (const char*), or IntPtr.Zero if failed.
    /// The string memory is managed by the engine and should NOT be freed by C#.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallConvention)]
    internal static extern IntPtr Resources_LoadText([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
}