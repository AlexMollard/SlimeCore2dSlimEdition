#pragma once

#include <cstdint>

using EntityId = uint32_t;
static constexpr EntityId INVALID_ENTITY = 0;

enum class Terrain : uint8_t
{
	Grass,
	Dirt,
	Water,
	Rock,
};

enum class Feature : uint8_t
{
	None,
	Tree,
	Ore,
	Bush,
};

enum class ItemType : uint8_t
{
	None,
	Food,
	Wood,
	Stone,
	OreChunk,
};

enum class EntityType : uint8_t
{
	PlayerSnake,
	NpcSnake,
	Monster,
};

struct Tile
{
	Terrain terrain = Terrain::Grass;
	Feature feature = Feature::None;
	ItemType item = ItemType::None;
	bool blocksMovement = false; // from terrain/feature
};
