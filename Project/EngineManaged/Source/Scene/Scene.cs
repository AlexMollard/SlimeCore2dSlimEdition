using System.Collections.Generic;

namespace EngineManaged.Scene
{
	public static class Scene
	{
		// ---------------------------------------------------------------------
		// Lifecycle Management
		// ---------------------------------------------------------------------

		/// <summary>
		/// Destroys the entity in the native engine.
		/// </summary>
		public static void Destroy(Entity e)
		{
			if (e.Id != 0)
			{
				Native.ObjectManager_Destroy(e.Id);
			}
		}

		/// <summary>
		/// Checks if the Entity ID is still valid in the native engine.
		/// </summary>
		public static bool IsAlive(Entity e)
		{
			return e.Id != 0 && Native.ObjectManager_IsAlive(e.Id);
		}

		// ---------------------------------------------------------------------
		// Iteration & Queries
		// ---------------------------------------------------------------------

		/// <summary>
		/// Gets the total count of native objects currently active.
		/// </summary>
		public static int Count => Native.ObjectManager_GetSize();

		/// <summary>
		/// Gets an entity wrapper for the object at the specific native index.
		/// </summary>
		public static Entity GetAt(int index)
		{
			ulong id = Native.ObjectManager_GetIdAtIndex(index);
			return new Entity(id);
		}

		/// <summary>
		/// Iterates over all active entities in the native scene.
		/// </summary>
		public static IEnumerable<Entity> Enumerate()
		{
			// We cache count to avoid calling C++ boundary too often, 
			// though be careful if you destroy objects *inside* this loop.
			int count = Count;
			for (int i = 0; i < count; i++)
			{
				yield return GetAt(i);
			}
		}
	}
}