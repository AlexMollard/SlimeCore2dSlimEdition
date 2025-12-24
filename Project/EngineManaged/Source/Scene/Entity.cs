using System;

namespace EngineManaged.Scene;

public record Entity
{
    public ulong Id { get; init; }

	public Entity(ulong id)
	{
		Id = id;
    }

    // Valid check
    public bool IsAlive => Id != 0 && Native.Entity_IsAlive(Id);

	// ---------------------------------------------------------------------
	// Lifecycle
	// ---------------------------------------------------------------------

	public static Entity Create()
	{
		return new Entity(Native.Entity_Create());
	}

	/// <summary>
	/// Marks the entity for destruction in the native engine.
	/// </summary>
	public void Destroy()
	{
		if (Id != 0) Native.Scene_Destroy(Id);
	}

	// ---------------------------------------------------------------------
	// Component Management
	// ---------------------------------------------------------------------

	public T GetComponent<T>() where T : struct, IComponent
	{
		if (!HasComponent<T>()) throw new ArgumentException($"Entity {Id} does not have component {typeof(T).Name}");
		T component = new T();
		component.EntityId = Id;
		return component;
	}

	public void AddComponent<T>() where T : struct, IComponent
	{
		var type = typeof(T);
		if (type == typeof(TransformComponent)) Native.Entity_AddComponent_Transform(Id);
		else if (type == typeof(SpriteComponent)) Native.Entity_AddComponent_Sprite(Id);
		else if (type == typeof(AnimationComponent)) Native.Entity_AddComponent_Animation(Id);
		else if (type == typeof(TagComponent)) Native.Entity_AddComponent_Tag(Id);
		else if (type == typeof(RelationshipComponent)) Native.Entity_AddComponent_Relationship(Id);
		else if (type == typeof(RigidBodyComponent)) Native.Entity_AddComponent_RigidBody(Id);
		else if (type == typeof(BoxColliderComponent)) Native.Entity_AddComponent_BoxCollider(Id);
		else if (type == typeof(CircleColliderComponent)) Native.Entity_AddComponent_CircleCollider(Id);
		else if (type == typeof(CameraComponent)) Native.Entity_AddComponent_Camera(Id);
		else if (type == typeof(AudioSourceComponent)) Native.Entity_AddComponent_AudioSource(Id);
		else throw new ArgumentException($"Component type {type.Name} is not supported.");
	}

	public bool HasComponent<T>() where T : struct, IComponent
	{
		var type = typeof(T);
		if (type == typeof(TransformComponent)) return Native.Entity_HasComponent_Transform(Id);
		else if (type == typeof(SpriteComponent)) return Native.Entity_HasComponent_Sprite(Id);
		else if (type == typeof(AnimationComponent)) return Native.Entity_HasComponent_Animation(Id);
		else if (type == typeof(TagComponent)) return Native.Entity_HasComponent_Tag(Id);
		else if (type == typeof(RelationshipComponent)) return Native.Entity_HasComponent_Relationship(Id);
		else if (type == typeof(RigidBodyComponent)) return Native.Entity_HasComponent_RigidBody(Id);
		else if (type == typeof(BoxColliderComponent)) return Native.Entity_HasComponent_BoxCollider(Id);
		else if (type == typeof(CircleColliderComponent)) return Native.Entity_HasComponent_CircleCollider(Id);
		else if (type == typeof(CameraComponent)) return Native.Entity_HasComponent_Camera(Id);
		else if (type == typeof(AudioSourceComponent)) return Native.Entity_HasComponent_AudioSource(Id);
		else throw new ArgumentException($"Component type {type.Name} is not supported.");
	}

	public void RemoveComponent<T>() where T : struct, IComponent
	{
		var type = typeof(T);
		if (type == typeof(TransformComponent)) Native.Entity_RemoveComponent_Transform(Id);
		else if (type == typeof(SpriteComponent)) Native.Entity_RemoveComponent_Sprite(Id);
		else if (type == typeof(AnimationComponent)) Native.Entity_RemoveComponent_Animation(Id);
		else if (type == typeof(TagComponent)) Native.Entity_RemoveComponent_Tag(Id);
		else if (type == typeof(RelationshipComponent)) Native.Entity_RemoveComponent_Relationship(Id);
		else if (type == typeof(RigidBodyComponent)) Native.Entity_RemoveComponent_RigidBody(Id);
		else if (type == typeof(BoxColliderComponent)) Native.Entity_RemoveComponent_BoxCollider(Id);
		else if (type == typeof(CircleColliderComponent)) Native.Entity_RemoveComponent_CircleCollider(Id);
		else if (type == typeof(CameraComponent)) Native.Entity_RemoveComponent_Camera(Id);
		else if (type == typeof(AudioSourceComponent)) Native.Entity_RemoveComponent_AudioSource(Id);
		else throw new ArgumentException($"Component type {type.Name} is not supported.");
	}
}