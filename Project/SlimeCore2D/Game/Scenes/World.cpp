#include "World.h"

#include <cassert>

World::World(int w, int h)
{
	m_w = w;
	m_h = h;
	m_tiles.resize((size_t)w * (size_t)h);
	m_occupancy.resize((size_t)w * (size_t)h, INVALID_ENTITY);
}

bool World::InBounds(const glm::ivec2& p) const
{
	return p.x >= 0 && p.y >= 0 && p.x < m_w && p.y < m_h;
}

int World::Index(const glm::ivec2& p) const
{
	return p.y * m_w + p.x;
}

Tile& World::At(const glm::ivec2& p)
{
	assert(InBounds(p));
	return m_tiles[(size_t)Index(p)];
}

const Tile& World::At(const glm::ivec2& p) const
{
	assert(InBounds(p));
	return m_tiles[(size_t)Index(p)];
}

EntityId World::Occupant(const glm::ivec2& p) const
{
	if (!InBounds(p)) return INVALID_ENTITY;
	return m_occupancy[(size_t)Index(p)];
}

void World::SetOccupant(const glm::ivec2& p, EntityId id)
{
	if (!InBounds(p)) return;
	m_occupancy[(size_t)Index(p)] = id;
}

bool World::IsBlocked(const glm::ivec2& p) const
{
	if (!InBounds(p)) return true;
	const Tile& t = At(p);
	if (t.blocksMovement) return true;
	const EntityId occ = Occupant(p);
	if (occ != INVALID_ENTITY)
	{
		const Entity* e = GetEntity(occ);
		if (e && e->blocks) return true;
	}
	return false;
}

EntityId World::CreateEntity(EntityType type, const glm::ivec2& p, bool blocks, const std::string& name)
{
	EntityId id = m_nextId++;
	Entity e;
	e.id = id;
	e.type = type;
	e.pos = p;
	e.blocks = blocks;
	e.name = name;
	if ((size_t)id - 1 >= m_entities.size())
		m_entities.resize((size_t)id);
	m_entities[(size_t)id - 1] = e;
	if (blocks) SetOccupant(p, id);
	return id;
}

Entity* World::GetEntity(EntityId id)
{
	if (id == INVALID_ENTITY) return nullptr;
	const size_t idx = (size_t)id - 1;
	if (idx >= m_entities.size()) return nullptr;
	return &m_entities[idx];
}

const Entity* World::GetEntity(EntityId id) const
{
	if (id == INVALID_ENTITY) return nullptr;
	const size_t idx = (size_t)id - 1;
	if (idx >= m_entities.size()) return nullptr;
	return &m_entities[idx];
}
