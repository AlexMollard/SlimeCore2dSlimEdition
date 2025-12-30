#pragma once

#include "Components.h"
#include <unordered_map>
#include <typeindex>
#include <memory>
#include <vector>
#include <algorithm>
#include <limits>

#ifdef max
#undef max
#endif // max


class IComponentPool {
public:
	virtual ~IComponentPool() = default;
	virtual void Remove(Entity entity) = 0;
	virtual bool Has(Entity entity) const = 0;
};

template<typename T>
class ComponentPool : public IComponentPool {
public:
	// Dense packed data for cache locality
	std::vector<T> m_Data;
	// Map from dense index to Entity ID (for back-reference)
	std::vector<Entity> m_Entities;
	// Sparse map from Entity ID to dense index
	std::vector<size_t> m_Sparse;

	static constexpr size_t NullIndex = std::numeric_limits<size_t>::max();

	void Add(Entity entity, T component) {
		if (Has(entity)) {
			m_Data[m_Sparse[entity]] = component;
			return;
		}

		// Ensure sparse array is big enough
		if (entity >= m_Sparse.size()) {
			m_Sparse.resize(entity + 1, NullIndex);
		}

		// Point sparse index to the new end of dense arrays
		m_Sparse[entity] = m_Data.size();
		m_Data.push_back(component);
		m_Entities.push_back(entity);
	}

	void Remove(Entity entity) override {
		if (!Has(entity)) return;

		size_t indexToRemove = m_Sparse[entity];
		size_t lastIndex = m_Data.size() - 1;

		// If not removing the last element, swap with the last one to keep dense
		if (indexToRemove != lastIndex) {
			Entity lastEntity = m_Entities[lastIndex];
			
			// Move last component to the hole
			m_Data[indexToRemove] = m_Data[lastIndex];
			m_Entities[indexToRemove] = lastEntity;
			
			// Update sparse map for the moved entity
			m_Sparse[lastEntity] = indexToRemove;
		}

		// Remove the last element
		m_Data.pop_back();
		m_Entities.pop_back();
		
		// Invalidate the removed entity
		m_Sparse[entity] = NullIndex;
	}

	T& Get(Entity entity) {
		return m_Data[m_Sparse[entity]];
	}

	T* TryGet(Entity entity) {
		if (!Has(entity)) return nullptr;
		return &m_Data[m_Sparse[entity]];
	}

	bool Has(Entity entity) const override {
		return entity < m_Sparse.size() && m_Sparse[entity] != NullIndex;
	}
};

class Registry {
public:
	Entity CreateEntity() {
		return m_NextEntityId++;
	}

	void DestroyEntity(Entity entity) {
		for (auto& pool : m_Pools) {
			if (pool) pool->Remove(entity);
		}
	}

	template<typename T>
	void AddComponent(Entity entity, T component) {
		GetPool<T>()->Add(entity, component);
	}

	template<typename T>
	void RemoveComponent(Entity entity) {
		GetPool<T>()->Remove(entity);
	}

	template<typename T>
	T& GetComponent(Entity entity) {
		return GetPool<T>()->Get(entity);
	}

	template<typename T>
	T* TryGetComponent(Entity entity) {
		return GetPool<T>()->TryGet(entity);
	}

	template<typename T>
	bool HasComponent(Entity entity) {
		return GetPool<T>()->Has(entity);
	}

	// Returns a reference to the dense list of entities with this component
	// Much faster than constructing a new vector
	template<typename T>
	const std::vector<Entity>& View() {
		return GetPool<T>()->m_Entities;
	}

private:
	Entity m_NextEntityId = 1;
	// Vector of pools, indexed by Component ID
	std::vector<std::shared_ptr<IComponentPool>> m_Pools;

	// Static counter for component types
	struct ComponentTypeCounter {
		static inline size_t Counter = 0;
	};

	template<typename T>
	static size_t GetComponentID() {
		static size_t id = ComponentTypeCounter::Counter++;
		return id;
	}

	template<typename T>
	std::shared_ptr<ComponentPool<T>> GetPool() {
		size_t id = GetComponentID<T>();
		
		if (id >= m_Pools.size()) {
			m_Pools.resize(id + 1);
		}

		if (!m_Pools[id]) {
			m_Pools[id] = std::make_shared<ComponentPool<T>>();
		}
		
		return std::static_pointer_cast<ComponentPool<T>>(m_Pools[id]);
	}
};
