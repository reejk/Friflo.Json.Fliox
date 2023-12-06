using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Fliox.Engine.Client;
using Friflo.Fliox.Engine.ECS;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Friflo.Fliox.Editor;

/// <summary>
/// Implementation of <see cref="EntityClient"/> commands.
/// </summary>
public class EditorService : IServiceCommands
{
    /// <remarks> Must be accessed only from main thread. </remarks>
    private readonly EntityStore    store;
        
    public EditorService(EntityStore store) {
        this.store = store;
    }
        
    [CommandHandler("editor.Collect")]
    private static Result<string> Collect(Param<int?> param, MessageContext context)
    {
        if (!param.GetValidate(out var nullableGeneration, out var error)) {
            return Result.ValidationError(error);
        }
        int generation = nullableGeneration ?? 1;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        GC.Collect(generation);
        
        double elapsedTicks    = stopwatch.ElapsedTicks;
        var duration        = 1000 * elapsedTicks / Stopwatch.Frequency;
        var msg = $"GC.Collect({generation}) - duration: {duration} ms";
        Console.WriteLine(msg);

        return msg;
    }
    
    [CommandHandler("editor.AddEntities")]
    private async Task<Result<AddEntitiesResult>> AddEntities(Param<AddEntities> param, MessageContext context)
    {
        if (!param.GetValidate(out var addEntities, out var error)) {
            return Result.ValidationError(error);
        }
        if (addEntities == null) {
            return Result.Error("addEntities payload is null");
        }
        if (addEntities.entities == null) {
            return Result.Error("missing entities array");
        }
        return await EditorUtils.InvokeAsync(() => Task.FromResult(AddInternal(addEntities)));
    }
    
    private Result<AddEntitiesResult> AddInternal (AddEntities addEntities)
    {
        if (!store.TryGetEntityByPid(addEntities.targetEntity, out var targetEntity)) {
            return Result.Error($"targetEntity not found. was: {addEntities.targetEntity}");
        }
        var entities    = addEntities.entities;
        var result      = ECSUtils.AddDataEntitiesToEntity(targetEntity, entities);
        
        var added       = new List<long?>(entities.Count);
        var missingPids = result.missingPids;
        foreach (var entity in entities) {
            if (result.addedEntities.Contains(entity.pid)) {
                added.Add(entity.pid);
                continue;
            }
            added.Add(null);
        }
        return new AddEntitiesResult {
            count           = addEntities.entities.Count,
            missingEntities = missingPids,
            addErrors       = result.addErrors,
            added           = added
        };
    }
}

