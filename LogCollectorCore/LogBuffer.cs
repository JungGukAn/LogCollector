using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogCollectorCore
{
    public class LogBuffer
    {
        public readonly Tag Tag;
        public readonly ImmutableList<Action<Tag, List<dynamic>>> Outputs;
        public readonly int FlushIntervalSeconds;
        public readonly int? FlushMaxCount;

        Queue<List<dynamic>> queues = new Queue<List<dynamic>>(1);

        DateTime prevTime = DateTime.UtcNow;
        double elapsedSeconds = 0;

        public LogBuffer(Tag tag, ImmutableList<Action<Tag, List<dynamic>>> outputs, int flushIntervalSeconds, int? flushMaxCount)
        {
            Tag = tag;
            Outputs = outputs;
            FlushIntervalSeconds = flushIntervalSeconds;
            FlushMaxCount = flushMaxCount;

            queues.Enqueue(new List<dynamic>(1));
        }

        public bool IsEmpty => queues.Count == 0;

        public bool isExpiredFlushSeconds => elapsedSeconds >= FlushIntervalSeconds;
        private bool isFullFlushMaxCount => queues.Count > 1;

        public void Add(dynamic log)
        {
            if (queues.TryPeek(out var list) == false)
            {
                list = new List<dynamic>(1);
                queues.Enqueue(list);
            }

            if (FlushMaxCount != null)
            {
                if (list.Count >= FlushMaxCount.Value)
                {
                    list = new List<dynamic>(1);
                    queues.Enqueue(list);
                }
            }

            list.Add(log);
        }

        public bool TryGetChunkForce(out List<dynamic> chunk)
        {
            return queues.TryDequeue(out chunk);
        }

        public bool TryGetChunk(out List<dynamic> chunk)
        {
            chunk = null;

            // For when time set is changed.
            double diffSeconds = (DateTime.UtcNow - prevTime).TotalSeconds;
            if (diffSeconds > 0)
            {
                elapsedSeconds += diffSeconds;
            }

            prevTime = DateTime.UtcNow;

            if (isExpiredFlushSeconds)
            {
                chunk = queues.Dequeue();
                elapsedSeconds = 0;
                return true;
            }

            if (isFullFlushMaxCount)
            {
                chunk = queues.Dequeue();
                return true;
            }

            return false;
        }
    }
}
