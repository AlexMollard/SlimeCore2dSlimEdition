#pragma once

#include <string>
#include <vector>

#include "glm.hpp"
#include "WorldTypes.h"

struct Entity
{
	EntityId id = INVALID_ENTITY;
	EntityType type = EntityType::Monster;
	glm::ivec2 pos = {0,0};
	bool blocks = true;

	// Optional metadata
	std::string name;
	int dialogueId = -1; // index into dialogue DB (if any)
};

class World
{
public:
	World(int w, int h);

	int Width() const { return m_w; }
	int Height() const { return m_h; }

	bool InBounds(const glm::ivec2& p) const;
	int Index(const glm::ivec2& p) const;

	Tile& At(const glm::ivec2& p);
	const Tile& At(const glm::ivec2& p) const;

	EntityId Occupant(const glm::ivec2& p) const;
	void SetOccupant(const glm::ivec2& p, EntityId id);

	bool IsBlocked(const glm::ivec2& p) const;

	// Entities
	EntityId CreateEntity(EntityType type, const glm::ivec2& p, bool blocks, const std::string& name = "");
	Entity* GetEntity(EntityId id);
	const Entity* GetEntity(EntityId id) const;
	std::vector<Entity>& Entities() { return m_entities; }
	const std::vector<Entity>& Entities() const { return m_entities; }

private:
	int m_w = 0;
	int m_h = 0;
	std::vector<Tile> m_tiles;
	std::vector<EntityId> m_occupancy; // 0 = none
	std::vector<Entity> m_entities;     // index = id-1
	EntityId m_nextId = 1;
};
