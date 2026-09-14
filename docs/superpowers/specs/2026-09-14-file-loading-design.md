# File loading rework: from hardcoded loaders to File/Garment menus

## Problem

AnimationTool is a MonoGame-based editing tool for animation assets (skeletons,
animations, garments), but it currently loads its one working asset via
hardcoded code. `AnimationsLoader.cs` (in the sibling `AnimationLoader`
project) has ~50 `LoadXxx()` methods (`LoadGrimoireDragon`,
`LoadWeddingCarrie`, `LoadRoboJetValkyrie`, `LoadKnight`, ...), each pointing
at absolute paths from old, unrelated side-projects
(`C:\Projects\languagegame\...`, `C:\Projects\weddinggame\...`, etc.), many of
which no longer exist on this machine.

`Game1.Initialize()` calls `AnimationManager.LoadContent()`, which calls
exactly one hardcoded `AnimationLoader.LoadXxx()` — currently
`LoadGrimoireDragon()` — with the rest commented out in
[AnimationManager.cs](../../AnimationTool/AnimationManager.cs). Switching what
asset the tool edits today means editing code, commenting/uncommenting a
line, and recompiling.

Since this is a personal tool (not shipping game code), it should use normal
file operations — File/Open dialogs — instead.

## Goals

- Add a **File** menu: New, Open Model, Open Animation, Save, Save As, Save
  As JSON.
- Add a **Garment** menu: New, Open.
- The tool starts blank (no hardcoded asset loads at startup).
- Achieve this with **zero changes** to the `AnimationLoader` or
  `AnimationLib` sibling projects — the ~50 legacy `LoadXxx()` methods stay
  exactly as they are (dead code / reference), untouched and uncalled. All
  new logic lives in the `AnimationTool` project.

## Non-goals

- Deleting or refactoring `AnimationsLoader.cs`'s legacy `LoadXxx()` methods.
- JSON support for *opening* model/animation/garment files (Open dialogs are
  XML-only; JSON stays available only via the existing Save As JSON path).
- New error-handling/validation UX for malformed or missing files — matches
  today's behavior (exceptions surface as-is). The one exception: starting
  blank is a genuinely new state that a couple of *other*, pre-existing files
  (`AnimationManager.Update(GameClock, Vector2)`, `GarmentTab.LoadContent()`)
  never had to handle, since a character was always hardcoded-loaded before
  any tab was reachable. Those two get a small guard each so navigating to
  the Garment/Test tab before opening anything doesn't crash — this is about
  making "start blank" itself safe, not about handling bad file content.
  Other tabs (e.g. `AnimationTab.cs`) may have similar latent assumptions;
  auditing every tab for this is out of scope.
- Automated test coverage — this is UI wiring in a MonoGame tool with no
  existing AnimationTool test project; verification is manual.

## Design

### Native file dialogs

**Revised during planning:** the NuGet package originally proposed here
(`NativeFileDialogSharp`) turned out to ship an x86_64-only macOS native
binary, which cannot load into this machine's `osx-arm64` .NET process — its
only other alternative on nuget.org (`NativeFileDialogExtendedSharp`) ships
no native binary at all. Both were verified by downloading and inspecting
the packages directly (`lipo -info` on the bundled `.dylib`).

Instead, add a small `FileDialogs` static helper class in `AnimationTool`
that shells out to macOS's `osascript`, using the `choose file` / `choose
file name` AppleScript commands — these are implemented on top of
`NSOpenPanel`/`NSSavePanel`, so the result is the same native dialog, with
no third-party dependency and no native compilation step. It exposes:

```csharp
static class FileDialogs
{
    // Returns the chosen POSIX path, or null if the user cancelled.
    public static string OpenFile(string extension, string prompt);
    // Returns the chosen POSIX path (always ending in ".extension"), or null if cancelled.
    public static string SaveFile(string extension, string defaultFileName, string prompt);
}
```

Native dialogs block synchronously; they're only invoked from menu button
click handlers, so no game-loop/async changes are needed. This is Mac-only —
if the tool is ever run on Windows/Linux, a second implementation would be
needed there.

### AnimationManager API

All new logic is added directly to `AnimationManager`
([AnimationManager.cs](../../AnimationTool/AnimationManager.cs)), operating
on the public surface `AnimationsLoader` already exposes
(`Animations`, `Garments`, `ModelFile`) plus the public methods already on
`AnimationContainer`/`Garment` (`ReadSkeletonXml`, `ReadAnimationXml`,
`WriteXml`, `WriteXmlFile`, `SkeletonFile`, `AnimationFile`, `GarmentFile`,
etc.) — no new public members are needed on `AnimationsLoader`,
`AnimationContainer`, or `Garment`.

```csharp
public void NewModel();        // AnimationLoader.LoadContent() — resets to an empty container + empty Garments list
public void OpenModel();       // dialog (xml) -> NewModel(), then ReadSkeletonXml onto the fresh container, sets AnimationLoader.ModelFile
public void OpenAnimation();   // dialog (xml) -> ReadAnimationXml onto the current Skeleton
public void NewGarment();      // new Garment(), AddToSkeleton() (no-op with zero fragments), Garments.Add
public void OpenGarment();     // dialog (xml) -> new Garment(path, Animations.Skeleton, Renderer), AddToSkeleton(), Garments.Add
public void Save();            // see "Save semantics" below
public void SaveAs();          // dialog (save, xml) -> point current model/animation at the new path, then write
public void SaveAsJson();      // dialog (save, json) -> WriteJson-equivalent path
```

These replace the current `Save()`/`SaveJson()` pair on `AnimationManager`
(today's is a thin passthrough to `AnimationLoader.Save()`/`SaveJson()`).

### Save semantics

Every save-able piece (skeleton+animation via `AnimationContainer`, and each
`Garment`) already tracks its own file path once loaded
(`SkeletonFile`, `AnimationFile`, `GarmentFile` — all public getters). `Save()`
checks each piece:

- If a piece has no path yet (new/never-saved), **Save transparently falls
  back to Save As for that piece** — the user is never blocked by a bare
  "Save" with nothing to write to.
- If a piece already has a path, `Save()` writes to it in place, matching
  today's `AnimationsLoader.Save()` behavior (`Animations.WriteXml()` +
  each `garment.WriteXml()` + each additional animation container's
  `WriteAnimationXml()`).

`SaveAs()` / `SaveAsJson()` always prompt, then set the relevant `Filename`
properties (`SkeletonFile`, `AnimationFile` on `AnimationContainer`; via
`Garment.WriteXmlFile(path)` for garments, which doesn't require setting the
private-set `GarmentFile` at all) before/while writing.

### Menu / UI wiring

`ToolsScreen.cs` already builds a hamburger-style menu (`Hamburger`,
`ContextMenuItem`) for Save/SaveJson/Undo/Redo/Copy/Paste/PasteSpecial/
Mirror/UnKey. Restructure the Save/SaveJson entries into two menus using the
same existing pattern:

- **File**: New, Open Model, Open Animation, Save, Save As, Save As JSON
- **Garment**: New, Open

Undo/Redo/Copy/Paste/Mirror/UnKey stay where they are. Each new menu item's
click handler calls the corresponding `AnimationManager` method, then calls
`ClearTabsAndScreens()` (already used elsewhere in `ToolsScreen`) so stale
skeleton/animation UI state doesn't linger after a New/Open.

### Startup

[Game1.cs:47](../../AnimationTool/Game1.cs:47) still calls
`AnimationManager.LoadContent()` during `Initialize()`. That method's body
changes from "call one hardcoded `AnimationLoader.LoadXxx()`" to just
`AnimationLoader.LoadContent()` (the existing empty-reset call) — the tool
now opens blank, and the user drives everything via File/Garment menus.

### Error handling

No new error handling is introduced. Malformed or missing files on Open
surface as unhandled exceptions, matching every existing `LoadXxx()` method
today (none of which have try/catch either). Dialog cancellation is a plain
no-op (the `FileDialogs` helper returns null; callers just return early).

### Testing

No automated test coverage — `AnimationTool` has no existing test project,
and this is MonoGame UI wiring, not library logic. Verification is manual:

1. Launch the tool; confirm it starts blank (no dragon/character loaded).
2. File > Open Model + Open Animation against a real existing asset (e.g.
   the Grimoire dragon files already on disk) — confirm it renders like the
   old hardcoded `LoadGrimoireDragon()` did.
3. Edit something (e.g. a bone), File > Save, confirm the file changed on
   disk and reloads correctly.
4. File > New, confirm the tool resets to a blank model.
5. Garment > Open against an existing garment file (e.g. one of the Grimoire
   dragon's part files) — confirm it attaches to the skeleton.
6. Garment > New, confirm an empty garment is added without error.
7. Save As / Save As JSON against a fresh path, confirm both file formats
   write out correctly.
