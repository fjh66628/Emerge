using UnityEngine;
using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> event_handlers = new();// 存储事件类型与对应的处理函数列表


    public static void Subscribe<T>(Action<T> handler) where T : GameEvent_Interface//订阅方法
    {
        var type = typeof(T);

        if (!event_handlers.TryGetValue(type, out var handlers))//初始化事件列表
        {
            handlers = new List<Delegate>();
            event_handlers[type] = handlers;
        }
        if (!handlers.Contains(handler))//添加事件处理函数,防止重复订阅
        {
            handlers.Add(handler);
        }
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : GameEvent_Interface//取消订阅方法
    {
        var type = typeof(T);

        if (event_handlers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                event_handlers.Remove(type);
            }
        }
    }

    public static void Publish<T>(T gameEvent) where T : GameEvent_Interface//发布事件方法
    {
        var type = typeof(T);

        if (event_handlers.TryGetValue(type, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T>)?.Invoke(gameEvent);
            }
        }
    }
}
