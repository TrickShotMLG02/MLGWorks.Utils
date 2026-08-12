# MLGWorks.Utils

> Reusable Unity runtime helpers, editor tooling, and lightweight gameplay patterns.

MLGWorks.Utils is an embedded Unity assembly for the infrastructure that tends to be rebuilt from project to project. It brings collections, pooling, timing, scene loading, dependency injection, logging, and state management into one focused toolkit.

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6%2B-222C37?logo=unity&logoColor=white" alt="Unity 6 or newer" />
  <img src="https://img.shields.io/badge/C%23-managed-blue?logo=csharp&logoColor=white" alt="C sharp" />
</p>

## ✨ At a glance

| Area | What it gives you |
| --- | --- |
| 🧺 **Collections** | Inspector-friendly dictionaries, hash sets, and lookups with editor drawers and validation. |
| ♻️ **Pooling** | Generic and Unity pools for `GameObject`, `Component`, `AudioSource`, and `ParticleSystem`. |
| ⏱️ **Timing** | `Timer`, `Cooldown`, and `RateLimiter` helpers for gameplay and UI behaviour. |
| 🗺️ **Scenes** | Scene references, coordinated loading/unloading, duplicate-load policies, and transitions. |
| 💉 **Dependency injection** | Service locator, injector, bootstrapper, initialization hooks, and auto-registration. |
| 🔁 **Patterns** | Event bus, subscriptions, disposables, singleton, and state-machine building blocks. |
| 🧩 **Unity extensions** | Helpers for Unity objects, components, transforms, UI, and randomized collections. |
| 📝 **Logging** | A shared logger with console output, file output, and Unity Editor integration. |

The runtime assembly is defined by [`MLGWorks.Utils.asmdef`](Utils/MLGWorks.Utils.asmdef). Editor-only code is kept under `Editor` folders and is excluded from player builds by Unity's assembly rules.

---

## 🧭 Explore the toolkit

The sections below describe the individual building blocks. Each area is small and composable, so a project can use only the helpers it needs.

<details>
<summary><strong>🧺 Serializable collections</strong></summary>

Unity does not serialize the standard .NET dictionary, hash set, or lookup types in the Inspector. These helpers provide serializable alternatives while retaining familiar collection semantics at runtime:

- `SerializableDictionary<TKey, TValue>` stores one value per key and supports duplicate-key validation policies.
- `SerializableHashSet<T>` stores unique values while preserving serialized Inspector order.
- `SerializableLookup<TKey, TValue>` maps one key to multiple values.

All three support adding, removing, clearing, bulk updates, predicate-based removal, read-only interfaces, and validation results. The accompanying editor drawers make the data editable directly in the Inspector. See [`Helpers/Collections`](Utils/Helpers/Collections) and the [`SerializableCollectionsExample`](../SerializableCollectionsExample.cs).

</details>

<details>
<summary><strong>♻️ Object pooling</strong></summary>

Pooling avoids repeated allocation and destruction for frequently spawned objects. The generic [`ObjectPool<T>`](Utils/Helpers/Pooling/Core/ObjectPool.cs) supports acquiring, releasing, prewarming, bulk operations, capacity inspection, and disposal.

Unity-specific pools wrap the same lifecycle for common scene objects:

- `GameObjectPool` activates instances on acquire and deactivates them on release.
- `ComponentPool<T>` manages components attached to pooled GameObjects.
- `AudioSourcePool` and `ParticleSystemPool` provide ready-to-use pools for effects.

All Unity pools expose prewarming and bulk acquire/release operations, making them useful for reducing gameplay-time allocation spikes.

</details>

<details>
<summary><strong>⏱️ Timing and throttling</strong></summary>

The timing helpers cover several common gameplay cases:

- `Timer` supports start, pause, resume, reset, and delta-time updates.
- `Cooldown` tracks whether an action is ready and provides `TryConsume()` for one-shot use.
- `RateLimiter` limits the number of acquisitions within a rolling time window.

These classes are state holders rather than `MonoBehaviour`s, so they can be driven by `Update`, a game loop, or a test with controlled time. See [`Helpers/Timing`](Utils/Helpers/Timing) and [`Timer.cs`](Utils/Helpers/Timer.cs).

</details>

<details>
<summary><strong>🗺️ Scene management</strong></summary>

[`SceneReference`](Utils/Helpers/SceneManagement/Core/SceneReference.cs) gives scenes a serialized reference that can be validated against Build Settings. [`SceneLoader`](Utils/Helpers/SceneManagement/Loading/SceneLoader.cs) then provides validated synchronous and asynchronous loading, unloading, loaded-state checks, and optional transitions.

For systems that may request several scenes at once, `SceneLoadCoordinator` tracks active operations and applies a configurable `DuplicateSceneLoadBehavior`. `SceneLoadOperation`, `SceneUnloadOperation`, and `SceneTransitionOperation` expose disposable operation wrappers that keep callbacks and cleanup together. See [`Helpers/SceneManagement`](Utils/Helpers/SceneManagement).

</details>

<details>
<summary><strong>💉 Dependency injection</strong></summary>

The dependency-injection utilities provide a lightweight service workflow without requiring a third-party container:

- `ServiceLocator` registers and resolves shared services.
- `Injector` applies members marked with `[Inject]`.
- `ServiceBootstrapper` and `DIInitializer` coordinate startup and initialization.
- `[DIService]` enables automatic service registration, while `IInitializable` marks services that need an initialization phase.

This is a good fit for project-level services such as save systems, audio, configuration, or analytics. The implementation lives under [`Dependency Injection`](Utils/Dependency%20Injection).

</details>

<details>
<summary><strong>🔁 Events, state machines, and lifecycle helpers</strong></summary>

- `EventBus` publishes strongly typed events through `IEvent`; `EventSubscription` and `CompositeDisposable` make unsubscription explicit and easy to group.
- `StateMachine` uses `IState`, `ITransition`, and `ITransitionCondition` to model state changes, with both conditional and unconditional transitions.
- `Singleton<T>` provides a Unity `MonoBehaviour` singleton base for scene components, while `PureSingleton<T>` provides lazy singleton access for regular C# classes.
- `DontDestroyOnLoad` is a small component helper for preserving objects between scene loads.

These patterns are independent: an event-driven system does not need to adopt the state machine or singleton helpers.

Both singleton bases live in the [`MLGWorks.Utils.Patterns.Singletons`](Utils/Patterns/Singletons) namespace. The Unity variant keeps scene lookup, duplicate destruction, and `Awake`/`OnDestroy` lifecycle handling; the pure C# variant has no Unity dependency and creates its instance on first access.

</details>

<details>
<summary><strong>🧩 Unity extensions</strong></summary>

The Unity extension classes add small, discoverable operations to familiar types:

- `RandomCollectionExtensions` shuffles lists, creates shuffled copies, and selects items by relative weight.
- `UnityObjectExtensions` handles Unity-object-specific null checks and convenience operations.
- `ComponentExtensions` simplifies component lookup and related component operations.
- `TransformExtensions` covers common transform positioning and hierarchy tasks.
- `UIExtensions` provides helpers for common UI component workflows.

The random collection API is demonstrated in the [Quick start](#quick-start) section below.

</details>

<details>
<summary><strong>📝 Logging</strong></summary>

`Logger` is a Unity singleton that forwards messages to the Unity console and writes session logs to a configurable location. It supports debug, info, warning, and error levels, queued writes, cleanup of old log files, and orderly shutdown. The editor integration lives in [`Utils/Logging/Editor`](Utils/Logging/Editor).

</details>

---

## 📦 Installation

### Add as a Git submodule

From the root of the Unity project that will consume the toolkit:

```bash
git submodule add https://github.com/TrickShotMLG02/MLGWorks.Utils.git Assets/MLGWorks.Utils
git commit -m "Add MLGWorks.Utils"
```

After cloning a project that already uses the submodule:

```bash
git submodule update --init --recursive
```

Unity imports the assembly automatically once the folder appears under `Assets/`.

### Update

Run these commands from the consuming project's root:

```bash
git submodule update --remote --merge Assets/MLGWorks.Utils
git add Assets/MLGWorks.Utils
git commit -m "Update MLGWorks.Utils"
```

To update every submodule instead:

```bash
git submodule update --remote --merge
```

### Remove

Run these commands from the consuming project's root:

```bash
git submodule deinit -f -- Assets/MLGWorks.Utils
git rm -f Assets/MLGWorks.Utils
git commit -m "Remove MLGWorks.Utils"
```

`git rm` removes the working-tree folder, the index entry, and the corresponding `.gitmodules` entry. `git submodule deinit` also removes the submodule's local registration from `.git/config`.

---

## 🚀 Quick start

The examples below use the `MLGWorks.Utils.Helpers.Unity` namespace. They can be placed in any script inside a Unity project that references the `MLGWorks.Utils` assembly.

### Shuffle a collection

[`RandomCollectionExtensions`](Utils/Helpers/Unity/RandomCollectionExtensions.cs) supports both in-place shuffling and non-mutating shuffled copies. Pass a `System.Random` when deterministic or test-controlled behaviour is useful.

```csharp
using System;
using System.Collections.Generic;
using MLGWorks.Utils.Helpers.Unity;

var cards = new List<string> { "A", "K", "Q", "J" };

cards.Shuffle();                       // Mutates cards in place.
List<string> copy = cards.Shuffled();  // Leaves the source sequence unchanged.

var seeded = new Random(42);
cards.Shuffle(seeded);
```

### Select by weight

Weights are parallel to the item list, must be finite and non-negative, and do not need to add up to `1`.

```csharp
using MLGWorks.Utils.Helpers.Unity;

var loot = new[] { "Potion", "Sword", "Gem" };
var weights = new[] { 70f, 25f, 5f };

string drop = loot.SelectWeighted(weights);

if (loot.TrySelectWeighted(weights, out string safeDrop))
{
    UnityEngine.Debug.Log($"Dropped: {safeDrop}");
}
```

Use `TrySelectWeighted` when an empty list or an all-zero weight set is valid. `SelectWeighted` throws `InvalidOperationException` in those cases.

---

## 🧪 Tests

Tests live in [`Utils.Tests`](Utils.Tests) and can be run from Unity's **Test Runner** window using the EditMode test platform. The project currently targets Unity `6000.3.21f1`; use that version or a compatible newer Unity 6 editor.

For CI, Unity's batch-mode test runner can be used with the path to your local Unity executable:

```bash
Unity -batchmode -nographics -quit -projectPath . -runTests -testPlatform editmode -testResults TestResults.xml
```

Replace `Unity` with the full path to the Unity executable when it is not available on `PATH`.

---

## 🗂️ Repository layout

```text
Assets/MLGWorks.Utils/
├── Utils/              Runtime and editor utility code
├── Utils.Tests/        NUnit tests for the utilities
├── Documentation/      Supporting documentation
└── README.md           This overview
```

---

## 🤝 Contributing

Keep reusable code inside the `MLGWorks.Utils` assembly, add or update a focused test under `Utils.Tests`, and let Unity regenerate IDE project files as needed. Avoid committing generated folders such as `Library`, `Temp`, `Logs`, or `obj`.

---

## 📄 License

No license file is currently included in this repository. Confirm the intended license with the repository owner before redistributing the package.
