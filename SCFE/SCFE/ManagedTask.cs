/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SCFE
{
    public class TaskPool<T>
    {
        private readonly Action<ManagedTask<T>, TaskResult> _taskCompletedCallback;
        private readonly List<ManagedTask<T>> _tasks = new List<ManagedTask<T>>();

        public TaskPool(Action<ManagedTask<T>, TaskResult> taskCompletedCallback)
        {
            _taskCompletedCallback = taskCompletedCallback;
        }

        public void AddTask(ManagedTask<T> task)
        {
            _tasks.Add(task);
            if (_taskCompletedCallback != null)
            {
                task.AddCallback(_taskCompletedCallback);
                task.AddCallback((t, tr) =>
                {
                    _tasks.Remove(t);
                });
            }

            task.Start();
        }

        public void AddTask(Func<ManagedTask<T>, TaskResult> taskFunc)
        {
            AddTask(new ManagedTask<T>(taskFunc));
        }

        public ImmutableList<ManagedTask<T>> GetTasks()
        {
            return _tasks.ToImmutableList();
        }
    }

    public class ManagedTask<T>
    {
        private readonly List<T> _publicationsList = new List<T>();
        private readonly Task<TaskResult> _task;

        public ManagedTask(Func<ManagedTask<T>, TaskResult> taskFunc)
        {
            _task = new Task<TaskResult>(() => taskFunc(this));
        }

        public void Publish(T result)
        {
            _publicationsList.Add(result);
        }

        public T GetLastPublication()
        {
            if (_publicationsList.Count > 0)
                return _publicationsList.Last();
            return default;
        }

        public void Start()
        {
            _task.Start();
        }

        public bool IsDone()
        {
            return _task.IsCompleted;
        }

        public TaskResult GetResult()
        {
            if (_task.IsCompleted)
                return _task.Result;
            return null;
        }

        public TaskResult WaitForResult()
        {
            _task.Wait();
            return _task.Result;
        }

        public void AddCallback(Action<ManagedTask<T>, TaskResult> taskCompletedCallback)
        {
            _task.ContinueWith(task1 => taskCompletedCallback(this, task1.Result));
        }
    }
}
