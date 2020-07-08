﻿using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class RuntimeTaskScheduler : TaskScheduler, IDisposable
    {
        private object aliveLock = new object();
        private bool alive = true;
        private LinkedList<Task> tasks = new LinkedList<Task>();
        private LinkedList<Task> priorityTasks = new LinkedList<Task>();
        private Thread thread;
        private AutoResetEvent newTaskEvent = new AutoResetEvent(false);
        private ManualResetEvent taskExecuted = new ManualResetEvent(false);

        public RuntimeTaskScheduler()
        {
            thread = new Thread(ExecutionThread);
            thread.Start();
        }

        private void ExecutionThread()
        {
            bool shouldStayAlive = true;
            Task currentlyExecuting = null;
            while (shouldStayAlive)
            {
                lock (priorityTasks)
                {
                    if (priorityTasks.First != null)
                    {
                        currentlyExecuting = priorityTasks.First.Value;
                        priorityTasks.RemoveFirst();

                    }
                }
                if(currentlyExecuting == null)
                {
                    lock (tasks)
                    {
                        if (tasks.First != null)
                        {
                            currentlyExecuting = tasks.First.Value;
                            tasks.RemoveFirst();
                        }
                    }
                }
                
                if(currentlyExecuting != null)
                {
                    taskExecuted.Reset();
                    TryExecuteTask(currentlyExecuting);
                    taskExecuted.Set();
                    currentlyExecuting = null;
                } else
                    if (shouldStayAlive)
                        newTaskEvent.WaitOne();

                lock (aliveLock)
                    shouldStayAlive = alive;
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(tasks, ref lockTaken);
                if (lockTaken) return tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(tasks);
            }
        }

        protected override void QueueTask(Task task)
        {
            newTaskEvent.Reset();
            lock (tasks)
                tasks.AddLast(task);
            newTaskEvent.Set();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            
            if (!taskWasPreviouslyQueued)
                QueueTask(task);
            else
            {
                bool inRegular, inPriority;
                lock (tasks)
                    inRegular = tasks.Contains(task);
                lock (priorityTasks)
                    inPriority = priorityTasks.Contains(task);
                if (!inPriority && !inRegular)
                    return false;
            }

            while (!task.IsCompleted)
                taskExecuted.WaitOne();

            return true;
        }

        public void PrioritizeTask(Task task)
        {
            lock (tasks)
            {
                tasks.Remove(task);
            }
            newTaskEvent.Reset();
            lock (priorityTasks)
                priorityTasks.AddLast(task);
            newTaskEvent.Set();
        }

        protected override bool TryDequeue(Task task)
        {
            lock (tasks)
                return tasks.Remove(task);
        }

        public void Dispose()
        {
            lock (aliveLock)
                alive = false;
        }

        public override int MaximumConcurrencyLevel => 1;

    }
}
