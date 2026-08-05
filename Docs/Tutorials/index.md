---
layout: home

hero:
  name: QfStudio.Godette.ReactiveUI
  text: ReactiveUI for Godot Engine
  tagline: Complete MVVM framework support for Godot 4.x
  actions:
    - theme: brand
      text: Quick Start
      link: /guide/getting-started
    - theme: alt
      text: API Reference
      link: https://qfstudio.github.io/QfStudio.Godette.ReactiveUI/docfx/

features:
  - title: Frame-based Rx Operators
    details: Godot frame-scheduled reactive operators including DebounceFrame, ThrottleFirstFrame, EveryUpdate, and more.
  - title: Control Binders
    details: Collection data binding for ItemList, OptionButton, Tree, TabBar, PopupMenu, and other Godot controls.
  - title: Property & Command Binding
    details: Full ReactiveUI-compatible Bind / OneWayBind / BindCommand experience with signal-driven and polling change notification.
  - title: Source Generator
    details: Auto-generate IViewFor<T> implementation via [GodotViewFor<T>] attribute — no boilerplate required.
  - title: Activation Lifecycle
    details: WhenActivated automatically tracks node entry/exit from the scene tree, with subscriptions cleaned up on deactivation.
  - title: View Locator & Routing
    details: GodotViewLocator bridges ReactiveUI view resolution with Godot PackedScene system, supporting RoutingState navigation.
---
