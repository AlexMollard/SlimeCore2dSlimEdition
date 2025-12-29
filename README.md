# SlimeCore2D (Slim Edition)

A hybrid 2D game engine combining a high-performance C++ core with flexible C# scripting/game logic.

---

## ✨ Overview
**SlimeCore2D (Slim Edition)** is a game engine project that demonstrates how to embed the .NET runtime within a C++ application. It separates low-level systems (Windowing, Rendering, Input) from high-level game logic, allowing you to write your game code in modern C#.

## 🏗 Architecture
The engine is split into two main components:

### 1. Native Host (`SlimeCore2D`)
- **Language**: C++
- **Responsibilities**:
  - Window creation and management (GLFW)
  - DirectX 11 context initialization
  - Hosting the .NET Runtime (`nethost`, `hostfxr`)
  - Exposing native API hooks for Rendering, Input, and ECS to the managed layer.

### 2. Managed Logic (`EngineManaged`)
- **Language**: C# (.NET 10)
- **Responsibilities**:
  - Game Loop (Update/Draw)
  - Game Modes (Factory, Snake, etc.)
  - Entity Component System (ECS) logic
  - Scene management and World generation

## ✅ Features
- **Hybrid C++/C# Architecture**: Best of both worlds - native performance for the core, managed productivity for gameplay.
- **Multiple Game Modes**:
  - **Factory**: A factory building simulation with tilemaps, resources, and actors.
  - **Snake**: A classic Snake game implementation.
  - **Dude**: A platformer/test mode.
- **2D Rendering**:
  - Batched Quad Renderer (DirectX 11).
  - TileMap rendering support.
  - Text rendering (FreeType).
- **Input System**: Unified input handling exposed to C#.

## 🛠 Build & Run
### Prerequisites
- **Visual Studio 2022** (with C++ and .NET Desktop Development workloads).
- **.NET 10 SDK** (or the specific version configured in `EngineManaged.csproj`).

### Steps
1. Open `Project/SlimeCore2D.sln` in Visual Studio.
2. Build the solution (this builds both the C++ host and the C# managed assembly).
   - The `EngineManaged` project is configured to copy its output to `Scripting/Publish/` where the C++ host expects it.
3. Set `SlimeCore2D` as the startup project.
4. Run the application.

## 🎮 Controls

### Factory Mode (Default)
- **W, A, S, D**: Move the player character.
- **Mouse Scroll**: Zoom in/out.
- **Left Click**: Place Conveyor Belt.
- **Right Click**: Remove Structure.

### Snake Mode
- **Arrow Keys**: Move the snake.

## 📂 Project Structure
- `Project/SlimeCore2D`: C++ Native Host source code.
- `Project/EngineManaged`: C# Game Logic source code.
- `Project/Dependencies`: Third-party libraries (GLFW, GLM, etc.).
- `Project/Shaders`: HLSL shaders (Vertex/Pixel).

## 📝 Notes
- The project uses `UnmanagedCallersOnly` to allow C++ to call directly into C# static methods without delegates, reducing overhead.
- Game modes can be switched in `Project/EngineManaged/GameHost.cs`.
