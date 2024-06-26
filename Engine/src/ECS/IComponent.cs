﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


// ReSharper disable RedundantTypeDeclarationBody
namespace Friflo.Engine.ECS;

/// <summary>
/// To enable adding a struct component to an <see cref="Entity"/> it need to implement <see cref="IComponent"/>.<br/>
/// <see cref="IComponent"/> types are <b>struct</b>s which only contains data / methods.<br/>
/// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#component">Example.</a>
/// </summary>
/// <remarks>
/// An <see cref="Entity"/> can contain multiple components but only one of each type.<br/>
/// </remarks>
public interface IComponent { }
