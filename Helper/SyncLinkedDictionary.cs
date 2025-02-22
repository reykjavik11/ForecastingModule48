﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ForecastingModule.Helper
{
    public class SyncLinkedDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();
        private readonly List<TKey> _keys = new List<TKey>();
        private readonly object _lock = new object(); // For synchronization

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_dictionary.TryAdd(key, value))
                {
                    _keys.Add(key); // Maintain insertion order
                }
                else
                {//FIXME here - looks like if key already exist - value is not updated
                    Update(key, value, value);
                }
            }
        }
        
        public bool Update(TKey key, TValue value, TValue oldValue)
        {
            lock (_lock)
            {
                if (_keys.Contains(key))
                {
                    return _dictionary.TryUpdate(key, value, oldValue);
                }
                else
                {
                    throw new ArgumentException("Can not update. Key does not exists.");
                }
            }
        }

        public bool Remove(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_dictionary.TryRemove(key, out value))
                {
                    _keys.Remove(key); // Maintain insertion order
                    return true;
                }
                return false;
            }
        }

        public TValue Get(TKey key)
        {
            TValue value;
            _dictionary.TryGetValue(key, out value);
            return value;
        }

        public TValue GetOrDefault(TKey key, TValue defaultValue)
        {
            TValue value;
            _dictionary.TryGetValue(key, out value);
            return value != null? value : defaultValue;
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                lock (_lock)
                {
                    return new List<TKey>(_keys); // Return a copy for thread-safety
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                lock (_lock)
                {
                    foreach (var key in _keys)
                    {
                        yield return _dictionary[key];
                    }
                }
            }
        }
    }
}
