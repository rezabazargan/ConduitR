// Copyright (c) ConduitR
// Fast-path publish invoker: cached per TNotification, small-N unrolled, minimal allocations.
// Requires: .NET 8+ for CollectionsMarshal.AsSpan.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConduitR.Abstractions;

namespace ConduitR.Internal;

internal static class PublishInvoker
{
    internal static class Cache<TNotification> where TNotification : INotification
    {
        // Delegate shape: (notification, strategy, ct, getInstances) -> Task
        public static readonly Func<TNotification, PublishStrategy, CancellationToken, Mediator.GetInstances, Task> Invoke
            = InvokeCore;

        private static Task InvokeCore(TNotification notification, PublishStrategy strategy, CancellationToken ct, Mediator.GetInstances gi)
        {
            // Resolve handlers once
            var handlersObj = gi(typeof(IEnumerable<INotificationHandler<TNotification>>));
            var handlers = handlersObj as IEnumerable<INotificationHandler<TNotification>> ?? Array.Empty<INotificationHandler<TNotification>>();

            // Fast path when the container returns a List<T>
            if (handlers is List<INotificationHandler<TNotification>> list)
            {
                if (list.Count == 0) return Task.CompletedTask;

                return strategy switch
                {
                    PublishStrategy.Parallel => Parallel(list, notification, ct),
                    PublishStrategy.StopOnFirstException => SequentialStrict(list, notification, ct),
                    _ => Sequential(list, notification, ct), // Sequential default
                };
            }

            // Fallback for arbitrary IEnumerable<T>
            return strategy switch
            {
                PublishStrategy.Parallel => Parallel(handlers, notification, ct),
                PublishStrategy.StopOnFirstException => SequentialStrict(handlers, notification, ct),
                _ => Sequential(handlers, notification, ct),
            };
        }

        // Sequential: skip awaits for already-completed tasks
// inside ConduitR.Internal.PublishInvoker.Cache<TNotification>

private static async Task Sequential(IList<INotificationHandler<TNotification>> list, TNotification n, CancellationToken ct)
{
    switch (list.Count)
    {
        case 0:
            return;

        case 1:
        {
            var t0 = list[0].Handle(n, ct);
            if (!t0.IsCompletedSuccessfully) await t0.ConfigureAwait(false);
            return;
        }

        case 2:
        {
            var t0 = list[0].Handle(n, ct);
            if (!t0.IsCompletedSuccessfully) await t0.ConfigureAwait(false);

            var t1 = list[1].Handle(n, ct);
            if (!t1.IsCompletedSuccessfully) await t1.ConfigureAwait(false);
            return;
        }

        default:
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i].Handle(n, ct);
                if (!t.IsCompletedSuccessfully) await t.ConfigureAwait(false);
            }
            return;
    }
}


        private static async Task Sequential(IEnumerable<INotificationHandler<TNotification>> handlers, TNotification n, CancellationToken ct)
        {
           switch (handlers.Count())
    {
        case 0:
            return;

        case 1:
        {
            var t0 = handlers.First().Handle(n, ct);
            if (!t0.IsCompletedSuccessfully) await t0.ConfigureAwait(false);
            return;
        }

        case 2:
        {
            var t0 = handlers.First().Handle(n, ct);
            if (!t0.IsCompletedSuccessfully) await t0.ConfigureAwait(false);

            var t1 = handlers.Skip(1).First().Handle(n, ct);
            if (!t1.IsCompletedSuccessfully) await t1.ConfigureAwait(false);
            return;
        }

        default:
            for (int i = 0; i < handlers.Count(); i++)
            {
                var t = handlers.ElementAt(i).Handle(n, ct);
                if (!t.IsCompletedSuccessfully) await t.ConfigureAwait(false);
            }
            return;
    }
        }

        // Strict sequential: always await (propagate first exception immediately)
        private static async Task SequentialStrict(IList<INotificationHandler<TNotification>> list, TNotification n, CancellationToken ct)
        {
            for (int i = 0; i < list.Count; i++)
            {
                await list[i].Handle(n, ct).ConfigureAwait(false);
            }
        }

        private static async Task SequentialStrict(IEnumerable<INotificationHandler<TNotification>> handlers, TNotification n, CancellationToken ct)
        {
            foreach (var h in handlers)
            {
                await h.Handle(n, ct).ConfigureAwait(false);
            }
        }

        // Parallel with small-N unroll and ArrayPool for tasks
        private static Task Parallel(IList<INotificationHandler<TNotification>> list, TNotification n, CancellationToken ct)
        {
            int len = list.Count;
            if (len == 0) return Task.CompletedTask;
            if (len == 1)
            {
                var t = list[0].Handle(n, ct);
                return t.IsCompletedSuccessfully ? Task.CompletedTask : t;
            }
            if (len == 2)
            {
                var t0 = list[0].Handle(n, ct);
                var t1 = list[1].Handle(n, ct);
                if (t0.IsCompletedSuccessfully && t1.IsCompletedSuccessfully) return Task.CompletedTask;
                if (t0.IsCompletedSuccessfully) return t1;
                if (t1.IsCompletedSuccessfully) return t0;
                return Task.WhenAll(t0, t1);
            }

            var pool = ArrayPool<Task>.Shared;
            var rented = pool.Rent(len);
            int count = 0;
            for (int i = 0; i < len; i++)
            {
                var t = list[i].Handle(n, ct);
                if (!t.IsCompletedSuccessfully) rented[count++] = t;
            }

            if (count == 0)
            {
                pool.Return(rented, clearArray: false);
                return Task.CompletedTask;
            }

            // Trim to exact length to avoid passing unused slots to WhenAll
            var arr = new Task[count];
            Array.Copy(rented, arr, count);
            pool.Return(rented, clearArray: false);
            return Task.WhenAll(arr);
        }

        private static Task Parallel(IEnumerable<INotificationHandler<TNotification>> handlers, TNotification n, CancellationToken ct)
        {
            // Fallback: collect only incomplete tasks
            List<Task>? tasks = null;
            foreach (var h in handlers)
            {
                var t = h.Handle(n, ct);
                if (!t.IsCompletedSuccessfully)
                    (tasks ??= new List<Task>(4)).Add(t);
            }
            return tasks is null ? Task.CompletedTask : Task.WhenAll(tasks);
        }
    }
}
