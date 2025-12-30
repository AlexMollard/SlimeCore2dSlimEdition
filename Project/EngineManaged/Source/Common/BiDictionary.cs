using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.Common;
public sealed class BiDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _forward = new();
    private readonly Dictionary<TValue, TKey> _reverse = new();

    public void Add(TKey key, TValue value)
    {
        if (_forward.ContainsKey(key) || _reverse.ContainsKey(value))
            throw new ArgumentException("Key or value already present");

        _forward[key] = value;
        _reverse[value] = key;
    }

    public TValue GetByKey(TKey key) => _forward[key];

    public TKey GetByValue(TValue value) => _reverse[value];

    public bool TryGetByKey(TKey key, out TValue value)
        => _forward.TryGetValue(key, out value!);

    public bool TryGetByValue(TValue value, out TKey key)
        => _reverse.TryGetValue(value, out key!);

    public void RemoveByKey(TKey key)
    {
        if (_forward.TryGetValue(key, out var value))
        {
            _forward.Remove(key);
            _reverse.Remove(value);
        }
    }

    public void RemoveByValue(TValue value)
    {
        if (_reverse.TryGetValue(value, out var key))
        {
            _reverse.Remove(value);
            _forward.Remove(key);
        }
    }
}

