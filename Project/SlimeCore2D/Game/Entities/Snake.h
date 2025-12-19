#pragma once

#include <array>
#include <deque>
#include <fstream>
#include <vector>

#include "Cell.h"
#include "Scenes/Dialogue.h"
#include "Rendering/Shader.h"
#include "Rendering/Text.h"
#include "Utils/ObjectManager.h"
#include "Scenes/World.h"

// NOTE: Kept the name 'Snake' so your existing Game2D.cpp does not need major changes.
// Internally this is now an open-world grid game with terrain, features, items, NPC snakes, and monsters.
class Snake
{
public:
	Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan);
	~Snake();

	void Update(float deltaTime);

private:
	// ---------- Constants (viewport size)
	static constexpr int VIEW_W = 80;
	static constexpr int VIEW_H = 50;

	// ---------- Simple agents
	struct SnakeAgent
	{
		EntityId headEntity = INVALID_ENTITY;
		std::deque<glm::ivec2> body; // body[0] is head position
		glm::ivec2 dir = {0, 0};
		glm::ivec2 lastDir = {0, 0};
		int pendingGrow = 0;
		bool isPlayer = false;
		bool alive = true;
	};

	struct MonsterAgent
	{
		EntityId entity = INVALID_ENTITY;
		glm::ivec2 dir = {0, 0};
		float thinkTimer = 0.0f;
	};

	// ---------- Gameplay
	void BuildViewportVisuals();
	void GenerateWorld();
	void PaintViewport();
	void UpdateCameraFollow();
	void HandleInput();
	void TickSimulation();

	bool TryMoveSnake(SnakeAgent& s, const glm::ivec2& newDir);
	bool StepSnake(SnakeAgent& s, bool allowWrap);
	void StepNpc(SnakeAgent& s);
	void StepMonster(MonsterAgent& m);

	void TryInteract();

	glm::ivec2 PlayerPos() const;
	glm::ivec2 WorldToViewOrigin() const; // top-left world tile currently at (0,0) view
	glm::ivec2 ClampToWorld(const glm::ivec2& p) const;

	// ---------- Rendering resources
	Renderer2D* m_renderer = nullptr;
	ObjectManager* m_objManager = nullptr;
	Camera* m_camera = nullptr;

	Cell m_view[VIEW_W][VIEW_H];
	float m_cellSize = 0.35f;
	float m_cellSpacing = 0.30f;

	Text* m_text = nullptr;
	Shader* m_textShader = nullptr;

	// ---------- World + entities
	World* m_world = nullptr;
	DialogueDB m_dialogues;

	SnakeAgent m_player;
	std::vector<SnakeAgent> m_npcs;
	std::vector<MonsterAgent> m_monsters;

	// ---------- Dialogue state
	bool m_inDialogue = false;
	EntityId m_talkingTo = INVALID_ENTITY;
	int m_dialogueNode = 0;

	// ---------- Timing
	float m_timer = 0.0f;
	float m_tick = 0.05f;
	float m_minTick = 0.06f;

	// ---------- Score / high scores (kept from original)
	int m_score = 0;
	std::array<int, 10> m_highScores{};
	std::fstream m_hsFile;

	// ---------- Colors
	glm::vec3 m_colGrass = glm::vec3(0.12f, 0.22f, 0.12f);
	glm::vec3 m_colDirt  = glm::vec3(0.25f, 0.18f, 0.10f);
	glm::vec3 m_colWater = glm::vec3(0.08f, 0.16f, 0.26f);
	glm::vec3 m_colRock  = glm::vec3(0.18f);
	glm::vec3 m_colTree  = glm::vec3(0.05f, 0.35f, 0.08f);
	glm::vec3 m_colOre   = glm::vec3(0.35f, 0.30f, 0.10f);
	glm::vec3 m_colFood  = glm::vec3(0.75f, 0.15f, 0.20f);
	glm::vec3 m_colPlayer= glm::vec3(0.10f, 0.75f, 0.15f);
	glm::vec3 m_colNpc   = glm::vec3(0.15f, 0.65f, 0.65f);
	glm::vec3 m_colMonster = glm::vec3(0.75f, 0.40f, 0.10f);
};
