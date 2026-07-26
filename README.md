# QfStudio.Godette.ReactiveUI

`QfStudio.Godette.ReactiveUI` provides [ReactiveUI](https://www.reactiveui.net/) integration for Godot Engine.

`QfStudio.Godette.ReactiveUI` implements ReactiveUI platform services for Godot Engine.

ReactiveUI is an open-source, composable MVVM (Model-View-ViewModel) framework for .NET platforms.

Generally, it takes 4 steps to implement `ReactiveUI` for a custom platform.

- Implement `IScheduler` for the UI thread.
- Implement `IActivationForViewFetcher` for view activation.
- Implement `ICreatesObservableForProperty` for property-change notification.
- Implement `ICommandBinderImplementation` for command binding.

QfStudio.Godette.ReactiveUI implements all of the above for the Godot Engine.

In the current version of QfStudio.Godette.ReactiveUI, it is designed to work with ReactiveUI v23. ReactiveUI v24-beta is not supported yet at the moment.

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

See [Developer.md](Developer.md).

## AI Disclosure

This project uses AI-assisted coding for suggestions and trivial tasks only. 
All code is vetted with best-effort human review. 
No dubious code is committed.
