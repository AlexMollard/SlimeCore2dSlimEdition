# SlimeCore2dSlimEdition

A minimal 2D engine and small Snake demo built with OpenGL (GLFW + GLEW), GLM, stb, and FreeType.

---

## ✨ Overview
**SlimeCore2d (Slim Edition)** is a lightweight 2D engine and demo project that demonstrates a simple renderer, physics scene, object manager, and a Snake game. It's intended as an educational sample and a base for experimenting with 2D systems and OpenGL in C++.

## ✅ Features
- Simple 2D renderer with batched quads and shader support
- Basic physics scene and object management
- Demo game: classic **Snake** with scoring and high-score saving (`Txt/HS.txt`)
- Text rendering using FreeType and custom text shaders
- Shaders stored in `Shaders/` and reusable engine components in `SlimeCore2D/`

## 🧩 Dependencies
The project includes the following libraries (found in `Project/Dependencies/`):
- GLFW (windowing and input)
- GLEW (OpenGL function loader)
- GLM (math library)
- stb (single-file libraries: image, truetype, etc.)
- FreeType (font rendering)

## 🛠 Build & Run (Windows)
1. Open `Project/SlimeCore2D.sln` with **Visual Studio 2019/2022**.
2. Select the platform (x64 or Win32) and a configuration (Debug/Release).
3. Build the solution and run from Visual Studio, or run the generated executable:
   - `Project/x64/Debug/SlimeCore2D.exe` (or `x86/Debug` depending on build).

Notes:
- The solution is configured to use the bundled `Dependencies/` folder for includes and libs.
- Ensure your system has a working OpenGL driver.

## ▶️ How to Play
- Use the **arrow keys** to move the snake.
- Collect food to grow and increase your score.
- High scores are saved to `Txt/HS.txt` automatically.
