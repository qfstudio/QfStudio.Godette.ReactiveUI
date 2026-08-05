---
layout: home

hero:
  name: QfStudio.Godette.ReactiveUI
  text: Godot 的 ReactiveUI 集成
  tagline: Godot 4.1+ 的 ReactiveUI MVVM 框架
  actions:
    - theme: brand
      text: 快速开始
      link: /zh/guide/getting-started
    - theme: alt
      text: API 参考
      link: https://qfstudio.github.io/QfStudio.Godette.ReactiveUI/docfx/

features:
  - title: 帧级 Rx 操作符
    details: 基于 Godot 帧调度的响应式操作符，包括 DebounceFrame、ThrottleFirstFrame、EveryUpdate 等。
  - title: 控件绑定器
    details: 为 ItemList、OptionButton、Tree、TabBar、PopupMenu 等 Godot 控件提供集合数据绑定。
  - title: 属性与命令绑定
    details: 与 ReactiveUI 完全兼容的 Bind / OneWayBind / BindCommand 体验，支持信号驱动和轮询两种属性变更通知。
  - title: Source Generator
    details: 通过 [GodotViewFor<T>] 特性自动生成 IViewFor<T> 实现，无需手写样板代码。
  - title: 激活生命周期
    details: WhenActivated 自动跟踪节点进入/离开场景树的状态，订阅随生命周期自动清理。
  - title: 视图定位与路由
    details: GodotViewLocator 桥接 ReactiveUI 视图解析与 Godot PackedScene 系统，支持 RoutingState 导航。
---
