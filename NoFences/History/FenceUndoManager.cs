using System;
using System.Collections.Generic;

namespace NoFences.History
{
    internal sealed class FenceUndoManager
    {
        private sealed class Entry
        {
            public Entry(string description, string[] paths)
            {
                Description = description;
                Paths = paths;
            }

            public string Description { get; }
            public string[] Paths { get; }
        }

        private readonly int capacity;
        private readonly List<Entry> history;

        public FenceUndoManager(int capacity = 20)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            history = new List<Entry>(capacity);
        }

        public bool CanUndo => history.Count > 0;
        public string NextDescription => CanUndo ? history[history.Count - 1].Description : null;

        public void Record(string description, IReadOnlyList<string> pathsBeforeChange)
        {
            if (pathsBeforeChange == null)
                throw new ArgumentNullException(nameof(pathsBeforeChange));
            if (history.Count == capacity)
                history.RemoveAt(0);

            var snapshot = new string[pathsBeforeChange.Count];
            for (int index = 0; index < pathsBeforeChange.Count; index++)
                snapshot[index] = pathsBeforeChange[index];
            history.Add(new Entry(description, snapshot));
        }

        public bool TryUndo(List<string> target)
        {
            if (!CanUndo)
                return false;
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            int index = history.Count - 1;
            Entry entry = history[index];
            history.RemoveAt(index);
            target.Clear();
            target.AddRange(entry.Paths);
            return true;
        }
    }
}
