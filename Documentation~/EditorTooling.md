# Editor tooling

ABC ships its complete authoring workflow in the package. Odin Inspector and other paid plugins are not required.

## Actor inspector

The Actor inspector groups authoring into three cards:

- Identity contains the actor tag with built-in presets and a custom stable-ID field.
- Blueprints is a reorderable composition list.
- Lifecycle and Update controls initialization and scheduled phases.

Validation appears before Play Mode for missing or duplicate blueprint references. During Play Mode, the Runtime card displays lifecycle state, data count, behaviour count, and context-sensitive Initialize, Kill, or Revive controls.

## Blueprint inspector

The blueprint inspector discovers every concrete provider through Unity `TypeCache` when the inspector opens. The add menu is namespace-grouped and disables module types already present in the blueprint.

Providers are embedded sub-assets. Their serialized fields are edited inline through cached `SerializedObject` instances. Reordering, creation, field changes, and deletion participate in Unity Undo.

The inspector reports:

- missing provider references;
- duplicate concrete module types;
- empty composition guidance.

Data and behaviour rows use distinct visual accents while preserving Unity Pro and Personal skin readability.

## ABC Dashboard

Open `Tools → ABC → Dashboard`.

The dashboard combines authoring and runtime diagnostics:

- scene Actor count;
- initialized and alive Actor count;
- live scene-free ActorWorld count;
- total ActorModel population;
- reserved world capacity;
- active exact-type query index count;
- one-click Actor, Actor World Runner, and Blueprint creation;
- one-click access to the Feature Scaffold;
- scene actor selection;
- quick-start code and documentation access.

ActorWorld diagnostics are compiled only in the Unity Editor and use weak references. They do not keep worlds alive and add no player runtime work.

## Feature Scaffold

Open `Tools → ABC → Feature Scaffold`.

The scaffold creates a coherent feature slice from one name and namespace:

- serializable actor data;
- behaviour with dependencies cached during `Initialize`;
- typed command and local listener;
- zero-boxing struct query action;
- optional data and behaviour providers for Blueprint authoring.

The preview shows the exact file set before generation. Output is deterministic normal C#, existing source is never overwritten, and every generated file can be edited or deleted without a generator package. Generation runs only in the editor and adds no runtime service, analyzer, DLL, reflection path, or player dependency.

## Menus

| Action | Menu |
| --- | --- |
| Create scene actor | `GameObject → ABC → Actor` |
| Create world runner | `GameObject → ABC → Actor World Runner` |
| Create actor blueprint | `Assets → Create → ABC → Blueprints → Actor` |
| Open dashboard | `Tools → ABC → Dashboard` |
| Generate a feature slice | `Tools → ABC → Feature Scaffold` |

## Assembly isolation

All custom editors live in `abc.unity.editor` with `Editor` as its only platform. Runtime internals needed for diagnostics are exposed with `InternalsVisibleTo`; no editor API enters `abc.unity` player assemblies.

## Visual verification

The editor assembly is compiled in both Unity 2022.3 LTS and Unity 6 validation projects. Before a public release, capture the Actor inspector, Blueprint inspector, and Dashboard in both Pro and Personal skins and store the selected images under `Documentation~/Images` so documentation always represents the shipping UI.
