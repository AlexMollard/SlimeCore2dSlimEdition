using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlimeCore.Source.World.Actors;

public class ActorManager<TEnum, TGameMode>
    where TEnum : Enum
    where TGameMode : IGameMode
{
    /// <summary>
    /// Used for Saving entities to a specific map.
    /// PLANNED: Background routines may use this to query the save state grid system
    /// </summary>
    public Guid GridReference { get; set; }

    /// <summary>
    /// Configurable action budget per tick
    /// </summary>
    public int ActBudget { get; set; }

    /// <summary>
    /// Short fire required preparation functions or spawn functions
    /// </summary>
    private Action<TGameMode, float>[] RequisiteRoutines { get; init; } = [];


    private readonly Dictionary<TEnum, HashSet<Actor<TEnum, TGameMode>>> _byType = new();

    private readonly HashSet<Actor<TEnum, TGameMode>> _all = new();

    private PriorityQueue<Actor<TEnum, TGameMode>, int> _active = new();
    private PriorityQueue<Actor<TEnum, TGameMode>, int> _next = new();


    public ActorManager(int actBudget, params Action<TGameMode, float>[] priorityActions)
    {
        ActBudget = actBudget;
        RequisiteRoutines = priorityActions;
    }
    public virtual void Tick(TGameMode mode, float deltaTime)
    {
        foreach (var routine in RequisiteRoutines)
        {
            routine.Invoke(mode, deltaTime);
        }

        int actBudget = ActBudget;
        while (_active.Count > 0)
        {
            var actor = _active.Dequeue();
            // Always run high priority actors (Priority 0 like Player)
            // Or if we have budget
            if (actor.Priority == 0 || actBudget-- > 0)
            {
                if (actor.TakeAction(mode, deltaTime))
                {
                    _next.Enqueue(actor, actor.Priority);
                }
                else
                {
                    Remove(actor);
                }
            }
            else
            {
                _next.Enqueue(actor, actor.Priority);
            }
        }
        // Swap queues
        (_active, _next) = (_next, _active);
    }

    public void Register(Actor<TEnum, TGameMode> actor)
    {
        _all.Add(actor);

        if (!_byType.TryGetValue(actor.Kind, out var set))
            _byType[actor.Kind] = set = new();

        set.Add(actor);

        _next.Enqueue(actor, actor.Priority);
    }
    public void Remove(Actor<TEnum, TGameMode> actor)
    {
        actor.Destroy();
        _all.Remove(actor);
        _byType[actor.Kind].Remove(actor);
    }

    public Actor<TEnum, TGameMode>[] ByType(TEnum type) 
        => _byType.TryGetValue(type, out var set) ? set.ToArray() : [];

    public int Count(TEnum kind)
        => _byType.TryGetValue(kind, out var set) ? set.Count : 0;

    public IEnumerable<Actor<TEnum, TGameMode>> Active(TEnum kind)
        => _byType.TryGetValue(kind, out var set) ? set : [];

    public void Destroy()
    {
        var actors = _all.ToArray();
        foreach (var actor in actors)
        {
            Remove(actor);
        }
        _next.Clear();
        _active.Clear();
    }

}
