# [![JSON Fliox](https://raw.githubusercontent.com/reejk/Friflo.Json.Fliox/main/docs/images/Json-Fliox.svg)](https://github.com/reejk/Friflo.Json.Fliox/blob/main/Engine/README.md)    **Friflo.Engine.ECS** ![splash](https://raw.githubusercontent.com/reejk/Friflo.Json.Fliox/main/docs/images/paint-splatter.svg)

## Package

This package is part of the project described below.


## Project

`Friflo.Engine.ECS` is an Entity Component System - ECS - optimized for performance and cache locality.

Additional features:
- Developer friendly / OOP like API by exposing the [Entity](https://github.com/friflo/Friflo.Engine-docs/blob/main/api/Entity.md)
  struct as the main interface.  
  The typical alternative of an ECS implementations is providing a `World` class and using `int` parameters as entity `id`s.
- Efficient component queries minimizing L1 cache misses with support for Vectorization (SIMD).
- Minimize times required for GC collection by using struct types for entities and components.  
  GC.Collect(1) < 0.8 ms when using 10.000.000 entities.
- Support tagging of entities and use them as a filter in queries.

More at **Friflo.Engine** [README.md](https://github.com/reejk/Friflo.Json.Fliox/blob/main/Engine/README.md)


## Links

- [Homepage](https://github.com/reejk/Friflo.Json.Fliox/blob/main/README.md)
- [NuGet Package](https://www.nuget.org/packages/Reejk.Just.ECS/)
- [License](https://github.com/reejk/Friflo.Json.Fliox/blob/main/LICENSE)
