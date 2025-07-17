using System;
using System.Collections.Generic;
using System.Timers;

namespace KpiApplication.Utils
{
    public class LruCache<TKey, TValue> where TValue : class
    {
        private readonly int _capacity;
        private readonly TimeSpan _expiration;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cacheMap;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly Timer _cleanupTimer;

        public LruCache(int capacity, TimeSpan expiration)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _expiration = expiration;
            _cacheMap = new Dictionary<TKey, LinkedListNode<CacheItem>>();
            _lruList = new LinkedList<CacheItem>();

            _cleanupTimer = new Timer(60_000); // cleanup mỗi phút
            _cleanupTimer.Elapsed += (s, e) => CleanupExpiredItems();
            _cleanupTimer.Start();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this)
            {
                if (_cacheMap.TryGetValue(key, out var node))
                {
                    if (IsExpired(node.Value))
                    {
                        RemoveInternal(key, node);
                        value = default;
                        return false;
                    }

                    node.Value.LastAccess = DateTime.UtcNow;
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default;
                return false;
            }
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            lock (this)
            {
                if (_cacheMap.TryGetValue(key, out var node))
                {
                    DisposeIfNeeded(node.Value.Value);
                    node.Value.Value = value;
                    node.Value.LastAccess = DateTime.UtcNow;

                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                else
                {
                    if (_cacheMap.Count >= _capacity)
                    {
                        var lru = _lruList.Last;
                        if (lru != null)
                        {
                            RemoveInternal(lru.Value.Key, lru);
                        }
                    }

                    var newItem = new CacheItem
                    {
                        Key = key,
                        Value = value,
                        LastAccess = DateTime.UtcNow
                    };
                    var newNode = new LinkedListNode<CacheItem>(newItem);
                    _lruList.AddFirst(newNode);
                    _cacheMap[key] = newNode;
                }
            }
        }

        public bool Remove(TKey key)
        {
            lock (this)
            {
                if (_cacheMap.TryGetValue(key, out var node))
                {
                    return RemoveInternal(key, node);
                }
                return false;
            }
        }

        public void Clear()
        {
            lock (this)
            {
                foreach (var node in _lruList)
                {
                    DisposeIfNeeded(node.Value);
                }
                _cacheMap.Clear();
                _lruList.Clear();
            }
        }

        private void CleanupExpiredItems()
        {
            lock (this)
            {
                var now = DateTime.UtcNow;
                var node = _lruList.Last;

                while (node != null)
                {
                    var prev = node.Previous;
                    if (IsExpired(node.Value))
                    {
                        RemoveInternal(node.Value.Key, node);
                    }
                    else
                    {
                        break;
                    }
                    node = prev;
                }
            }
        }

        private bool IsExpired(CacheItem item)
        {
            return (DateTime.UtcNow - item.LastAccess) > _expiration;
        }

        private bool RemoveInternal(TKey key, LinkedListNode<CacheItem> node)
        {
            DisposeIfNeeded(node.Value.Value);
            _lruList.Remove(node);
            return _cacheMap.Remove(key);
        }

        private void DisposeIfNeeded(object obj)
        {
            if (obj is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch { /* optional log */ }
            }
        }

        private class CacheItem
        {
            public TKey Key;
            public TValue Value;
            public DateTime LastAccess;
        }
    }
}
