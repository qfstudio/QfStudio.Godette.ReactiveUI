# QfStudio.Godette.ReactiveUI

[ReactiveUI](https://www.reactiveui.net/) is a composable, cross-platform MVVM (Model-View-ViewModel) framework for .NET. It uses reactive extensions to bind UI elements to ViewModel properties and commands, keeping views and business logic cleanly separated.

`QfStudio.Godette.ReactiveUI` provides the platform services that make ReactiveUI work with Godot Engine - scheduling, activation, property-change notification, and command binding. See [Developer.md](Docs/Developer.md) for implementation details.

In the current version of QfStudio.Godette.ReactiveUI, it is designed to work with ReactiveUI v23. ReactiveUI v24 released at July 26th this year is not supported yet at the moment.

## Installation

TODO

## Usage

TODO

## Notes

**Activation semantics**: A view is activated (`true`) when it is in the scene tree **and** ready (all children initialized). This is semantically equivalent to Avalonia's `AttachedToVisualTree` / `DetachedFromVisualTree`.

## Alternatives

- [**R3**](https://github.com/Cysharp/R3) A zero-allocation, high-performance Rx.NET alternative. If you prefer building apps with ReactiveProperty or don't want to apply the full MVVM pattern, you can try R3. Note that R3 can also be used with ReactiveUI v24.

---

## Development

See [Developer.md](Docs/Developer.md).

## AI Disclosure

This project uses AI-assisted coding for suggestions and trivial tasks only. 
All code is vetted with best-effort human review. 
No dubious code is committed.
