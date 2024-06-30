﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal interface IRelationComponent : IComponent { }

/// <summary>
/// A relation component enables adding multiple components of the same type to an entity.<br/>
/// The components added to a single entity build a set of components using the relation <typeparamref name="TKey"/> as unique identifier.  
/// </summary>
/// <typeparam name="TKey">The key defining a unique relation component.</typeparam>
/// <remarks>
/// A relation component enables:
/// <list type="bullet">
///   <item>
///     Return all relation components of an entity using <see cref="RelationExtensions.GetRelations{TComponent}"/>.
///   </item>
///   <item>
///     Remove a specific relation component by key using <see cref="RelationExtensions.RemoveRelation{T,TKey}"/>.
///   </item>
/// </list>
/// </remarks>
internal interface IRelationComponent<out TKey> : IRelationComponent
{
    /// <summary>
    /// Returns the key of a unique relation component.
    /// </summary>
    TKey GetRelationKey();
}