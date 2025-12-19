#pragma once
#include "Quad.h"
#include "Texture.h"
#include <vector>
#include "Core/Camera.h"

class Renderer2D
{
public:
	Renderer2D(Camera* camera);
	~Renderer2D();

	static void Init();
	static void ShutDown();

	void AddObject(GameObject* newObject);

	Texture* LoadTexture(std::string dir);

	void Draw();
	void DrawUI();

	Shader* GetBasicShader();
	static void SetShader(Shader* shader);

	static void DrawUIQuad(glm::vec2 pos = glm::vec2(0), int layer = 1, glm::vec2 size = glm::vec2(1), glm::vec3 color = glm::vec3(1), Texture* texture = nullptr);

	static void setActiveRegion(Texture* texture, int regionIndex, int spriteWidth);

	static void DrawQuad(glm::vec3 position, glm::vec2 size, glm::vec4 color);
	static void DrawQuad(glm::vec2 position, glm::vec2 size, glm::vec4 color) { DrawQuad(glm::vec3(position, -0.9f), size, color); };
	static void DrawQuad(glm::vec3 position, glm::vec2 size, glm::vec4 color, Texture* texture, int frame = 0, int spriteWidth = 16);

	void RemoveQuad(GameObject* object);
	int GetObjectIndex(GameObject* object);

	static void BeginBatch();
	static void EndBatch();
	static void Flush();

	static Shader* basicShader;
private:
	glm::vec3 currentColor = glm::vec3(-404);
	glm::mat4 UIMatrix = glm::ortho<float>(16, -16, 9, -9, 2, 4);
	Shader* currentShader = nullptr;
	Texture* currentTexture = nullptr;

	std::vector<GameObject*> objectPool;
	std::vector<Texture*> texturePool;
	std::vector<Shader*> shaderPool;

	static Shader* UIShader;
	static Camera* camera;

	static std::vector<glm::vec2> UVs;
};