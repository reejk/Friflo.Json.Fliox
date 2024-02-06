﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection.Emit;
using System.Threading;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal abstract class JobTask {
    internal abstract void Execute();
}

/// <remarks>
/// The main goals of the <see cref="ParallelJobRunner"/>:<br/>
/// - Minimize calls to synchronization primitives.<br/>
/// - Use cheap synchronization primitives such as:
///   <see cref="ManualResetEventSlim"/>, <see cref="Interlocked"/> and <see cref="Volatile"/>.<br/>
/// - Minimize thread context switches caused by <see cref="ManualResetEventSlim"/> in case calling
///   <see cref="ManualResetEventSlim.Wait()"/> when the event is not signaled.<br/>
/// </remarks>
internal sealed class ParallelJobRunner
{
#region fields
    private  readonly   ManualResetEventSlim    startWorkers        = new (false);
    private  readonly   ManualResetEventSlim    allWorkersFinished  = new (false);
    private             int                     allFinishedBarrier;
    private             int                     finishedWorkerCount;
    private             bool                    workersStarted;
    private             JobTask[]               jobTasks;
    internal readonly   int                     workerCount;
    #endregion
    
    internal ParallelJobRunner(int threadCount) {
        workerCount = threadCount - 1;
    }
    
    private void StartWorkers()
    {
        workersStarted = true;
        for (int index = 0; index < workerCount; index++)
        {
            var worker = new ParallelJobWorker(index);
            var thread = new Thread(() => RunWorker(worker)) {
                Name            = $"ParallelJobWorker - {index}",
                IsBackground    = true
            };
            thread.Start();
        }
    }
    
    // ------------------------------------------ job thread -------------------------------------------
    internal void ExecuteJob(JobTask[] tasks, JobTask task0)
    {
        if (!workersStarted) {
            StartWorkers();
        }
        jobTasks = tasks;
        
        Volatile.Write(ref finishedWorkerCount, 0);
        
        // set before increment allFinishedBarrier to prevent blocking worker thread
        startWorkers.Set(); // all worker threads start running ...

        Interlocked.Increment(ref allFinishedBarrier);

        task0.Execute();
            
        allWorkersFinished.Wait();

        allWorkersFinished.Reset();
        
        if (startWorkers.IsSet) throw new InvalidOperationException("startWorkers.IsSet");
        
        jobTasks = null;
    }
    
    // ----------------------------------------- worker thread -----------------------------------------
    private void RunWorker(ParallelJobWorker worker)
    {
        
        var barrier = 0;
        var index   = worker.index;
        goto Label;
        
    Next:
        startWorkers.Wait();
        
        // --- execute task
        var task = jobTasks[index];
        task.Execute();
            
        // ---
        var count = Interlocked.Increment(ref finishedWorkerCount);
        if (count > workerCount) throw new InvalidOperationException($"unexpected count: {count}");
        if (count == workerCount)
        {
            startWorkers.Reset();
            allWorkersFinished.Set();
        }
    Label:
        // --- wait until a task is scheduled ...
        // spin wait for event to prevent preempting thread execution on: startWorkers.Wait()
        while (barrier == Volatile.Read(ref allFinishedBarrier)) {
            // Thread.SpinWait(1);
        }
        barrier++;
        goto Next;
    }
}

internal sealed class ParallelJobWorker
{
    internal readonly    int                 index;
    
    internal ParallelJobWorker(int index) {
        this.index  = index;
    }
    
    #region test
    /* private const int SpinMax = 5000;

    private void WaitStartWorkers()
    {
        var startWorkers = jobRunner.startWorkers;
        for (int n = 0; n < SpinMax; n++)
        {
            if (startWorkers.IsSet) {
                ++passCount;
                return;
            }
        }
        startWorkers.Wait();
        if (++waitCount % 100 == 0) Console.WriteLine($"passCount: {passCount},  waitCount: {waitCount}");
    }

    int passCount;
    int waitCount;
    long spinWaitCount; */
    #endregion
}










