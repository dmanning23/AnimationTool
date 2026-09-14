# File Loading Menus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace AnimationTool's hardcoded startup character-loading with a File menu (New, Open Model, Open Animation, Save, Save As, Save As JSON) and a Garment menu (New, Open), all backed by native macOS file dialogs.

**Architecture:** A new Mac-only `FileDialogs` static helper shells out to `osascript` for native Open/Save dialogs. All new logic lives in `AnimationManager` (calling the already-public `AnimationsLoader`/`AnimationContainer`/`Garment`/`Filename` APIs directly), wired up through two new `Hamburger` dropdown menus in `ToolsScreen`. Zero changes to the `AnimationLoader` or `AnimationLib` sibling projects.

**Tech Stack:** C#/.NET 8, MonoGame DesktopGL, MenuBuddy (UI widgets), FilenameBuddy (`Filename`), AnimationLib (`AnimationContainer`/`Garment`), macOS `osascript`.

**Spec:** [docs/superpowers/specs/2026-09-14-file-loading-design.md](../specs/2026-09-14-file-loading-design.md)

## Global Constraints

- Zero changes to the `AnimationLoader` project (`../../AnimationLoader/AnimationLoader/AnimationLoader.csproj`) or the `AnimationLib` project — every legacy `LoadXxx()` method stays exactly as-is, untouched and uncalled.
- Open dialogs (Model/Animation/Garment) are XML-only. Save supports XML (Save/Save As) and JSON (Save As JSON) as separate, explicit menu items — no format auto-detection.
- File dialogs are native macOS dialogs shown via `osascript` (`choose file` / `choose file name`), not a third-party NuGet package — the only maintained option (`NativeFileDialogSharp`) ships an x86_64-only macOS binary that cannot load into this machine's `osx-arm64` process.
- Save falls back to Save As, per-piece (skeleton+animation together; each garment independently), whenever that piece has no known file path yet — a bare Save is never blocked.
- No new error handling for malformed/missing files (matches today's total absence of error handling in every `LoadXxx()` method) — exceptions surface as-is. The two exceptions are the blank-start guards in Task 2, which exist only because "start blank" is a new capability this plan introduces, not because of file-content errors.
- No automated tests — `AnimationTool` has no test project, and this is MonoGame UI wiring. Verification is manual, specified per-task below.

---

### Task 1: Add the `FileDialogs` helper

**Files:**
- Create: `AnimationTool/FileDialogs.cs`

**Interfaces:**
- Produces: `AnimationTool.FileDialogs.OpenFile(string extension, string prompt) : string` (returns an absolute POSIX path, or `null` if cancelled) and `AnimationTool.FileDialogs.SaveFile(string extension, string defaultFileName, string prompt) : string` (returns an absolute POSIX path always ending in `.{extension}`, or `null` if cancelled). Task 2 calls both.

This class has no MonoGame dependency — it only uses `System.Diagnostics.Process` to shell out to `osascript`, which drives the real native Open/Save panel (macOS's `choose file`/`choose file name` AppleScript commands are implemented on top of `NSOpenPanel`/`NSSavePanel`).

- [ ] **Step 1: Write `FileDialogs.cs`**

```csharp
using System;
using System.Diagnostics;

namespace AnimationTool
{
    /// <summary>
    /// Native macOS Open/Save dialogs, shown via osascript's "choose file" /
    /// "choose file name" commands (backed by NSOpenPanel/NSSavePanel).
    /// Mac-only: this tool currently only runs on macOS.
    /// </summary>
    public static class FileDialogs
    {
        /// <summary>
        /// Shows a native Open dialog filtered to the given extension (no dot, e.g. "xml").
        /// Returns the chosen POSIX path, or null if the user cancelled.
        /// </summary>
        public static string OpenFile(string extension, string prompt)
        {
            var script = $"POSIX path of (choose file with prompt \"{EscapeForAppleScript(prompt)}\" of type {{\"{EscapeForAppleScript(extension)}\"}})";
            return RunOsaScript(script);
        }

        /// <summary>
        /// Shows a native Save dialog with the given default filename. The returned
        /// path always ends in "." + extension, even if the user typed a different one.
        /// Returns null if the user cancelled.
        /// </summary>
        public static string SaveFile(string extension, string defaultFileName, string prompt)
        {
            var script = $"POSIX path of (choose file name with prompt \"{EscapeForAppleScript(prompt)}\" default name \"{EscapeForAppleScript(defaultFileName)}\")";
            var path = RunOsaScript(script);
            if (null == path)
            {
                return null;
            }

            var suffix = "." + extension;
            if (!path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                path += suffix;
            }

            return path;
        }

        private static string RunOsaScript(string script)
        {
            var startInfo = new ProcessStartInfo("osascript")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(script);

            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // Non-zero exit means the user cancelled (AppleScript error -128)
                // or something else went wrong; either way there's no path to use.
                return null;
            }

            return output.Trim();
        }

        private static string EscapeForAppleScript(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
```

- [ ] **Step 2: Build the project**

Run: `dotnet build AnimationTool/AnimationTool.csproj`
Expected: Build succeeds (this file has no dependents yet, so this just confirms it compiles standalone).

- [ ] **Step 3: Commit**

```bash
git add AnimationTool/FileDialogs.cs
git commit -m "$(cat <<'EOF'
Add native macOS file dialog helper

Shells out to osascript's choose file/choose file name commands
(backed by NSOpenPanel/NSSavePanel) instead of a third-party NuGet
package, since the only maintained option ships an x86_64-only
macOS binary that can't load on this arm64 machine.
EOF
)"
```

---

### Task 2: Add New/Open/Save methods to `AnimationManager`, start blank

**Files:**
- Modify: `AnimationTool/AnimationManager.cs`
- Modify: `AnimationTool/Tabs/GarmentTab.cs:70`

**Interfaces:**
- Consumes: `FileDialogs.OpenFile(string, string) : string`, `FileDialogs.SaveFile(string, string, string) : string` (Task 1).
- Produces: `AnimationManager.NewModel()`, `.OpenModel()`, `.OpenAnimation()`, `.NewGarment()`, `.OpenGarment()`, `.Save()`, `.SaveAs()`, `.SaveAsJson()` — all `public void`, no parameters. Task 3's menu handlers call these directly. Replaces the old `AnimationManager.Save()` / `.SaveJson()` pair (same `Save()` name, new behavior; `SaveJson()` is gone, replaced by `SaveAsJson()`).

This task makes `AnimationManager.LoadContent()` start with a blank model instead of hardcoding `AnimationLoader.LoadGrimoireDragon()`. That surfaces a pre-existing gap in two other files: `AnimationManager.Update(GameClock, Vector2)` and `GarmentTab.LoadContent()` both assume a character is already loaded (true for every hardcoded `LoadXxx()`, never true for a fresh blank start). This task adds a guard to each so navigating to the Garment or Test tab before opening anything doesn't crash. (Other tabs, e.g. `AnimationTab.cs`, may have similar latent assumptions that were never exercised before since the tool always launched with a hardcoded character; auditing every tab for this is out of scope here — see the spec's non-goals.)

- [ ] **Step 1: Replace `LoadContent()` to start blank**

In `AnimationTool/AnimationManager.cs`, the current `LoadContent()` method (lines 116-175) ends with ~50 commented-out `AnimationLoader.LoadXxx()` lines, one active `AnimationLoader.LoadGrimoireDragon()` call, and then:

```csharp
            var animation = Animations.Animations.First().Key;
            Animations.SetAnimation(animation, EPlayback.Forwards);

            SelectedAnimationContainer = Animations;
        }
```

Replace the entire body from `AnimationLoader = new AnimationsLoader(Renderer, 1f);` through the end of the method with:

```csharp
            AnimationLoader = new AnimationsLoader(Renderer, 1f);
            NewModel();
        }
```

(`NewModel()`, added in Step 2, both resets the loader and sets `SelectedAnimationContainer`, so those lines don't need to be duplicated here.)

- [ ] **Step 2: Replace `Save()`/`SaveJson()` with the new File/Garment methods**

In `AnimationTool/AnimationManager.cs`, replace the existing `Save()` and `SaveJson()` methods (lines 238-246):

```csharp
        public void Save()
        {
            AnimationLoader.Save();
        }

        public void SaveJson()
        {
            AnimationLoader.SaveJson();
        }
```

with:

```csharp
        public void NewModel()
        {
            AnimationLoader.LoadContent();
            SelectedAnimationContainer = Animations;
        }

        public void OpenModel()
        {
            var path = FileDialogs.OpenFile("xml", "Open Model");
            if (null == path)
            {
                return;
            }

            NewModel();

            var modelFile = new Filename { File = path };
            AnimationLoader.ModelFile = modelFile;
            AnimationLoader.Animations.ReadSkeletonXml(modelFile, Renderer);
        }

        public void OpenAnimation()
        {
            var path = FileDialogs.OpenFile("xml", "Open Animation");
            if (null == path)
            {
                return;
            }

            var animationFile = new Filename { File = path };
            AnimationLoader.Animations.ReadAnimationXml(animationFile);

            var animation = AnimationLoader.Animations.Animations.First().Key;
            AnimationLoader.Animations.SetAnimation(animation, EPlayback.Forwards);
        }

        public void NewGarment()
        {
            var garment = new Garment(AnimationLoader.Animations.Scale);
            garment.AddToSkeleton();
            AnimationLoader.Garments.Add(garment);
        }

        public void OpenGarment()
        {
            var path = FileDialogs.OpenFile("xml", "Open Garment");
            if (null == path)
            {
                return;
            }

            var garmentFile = new Filename { File = path };
            var garment = new Garment(garmentFile, AnimationLoader.Animations.Skeleton, Renderer);
            garment.AddToSkeleton();
            AnimationLoader.Garments.Add(garment);
        }

        public void Save()
        {
            if (!HasPath(AnimationLoader.Animations.SkeletonFile) || !HasPath(AnimationLoader.Animations.AnimationFile))
            {
                SaveAs();
                return;
            }

            AnimationLoader.Animations.WriteXml();
            SaveGarments();
        }

        public void SaveAs()
        {
            var skeletonPath = FileDialogs.SaveFile("xml", "Model.xml", "Save Model As");
            if (null == skeletonPath)
            {
                return;
            }

            var animationPath = FileDialogs.SaveFile("xml", "Animations.xml", "Save Animation As");
            if (null == animationPath)
            {
                return;
            }

            AnimationLoader.Animations.WriteSkeletonXml(new Filename { File = skeletonPath });
            AnimationLoader.Animations.WriteAnimationXml(new Filename { File = animationPath });
            AnimationLoader.ModelFile = AnimationLoader.Animations.SkeletonFile;

            SaveGarments();
        }

        public void SaveAsJson()
        {
            var skeletonPath = FileDialogs.SaveFile("json", "Model.json", "Save Model As JSON");
            if (null == skeletonPath)
            {
                return;
            }

            var animationPath = FileDialogs.SaveFile("json", "Animations.json", "Save Animation As JSON");
            if (null == animationPath)
            {
                return;
            }

            AnimationLoader.Animations.WriteSkeletonJson(new Filename { File = skeletonPath });
            AnimationLoader.Animations.WriteAnimationJson(new Filename { File = animationPath });
            AnimationLoader.ModelFile = AnimationLoader.Animations.SkeletonFile;

            foreach (var garment in AnimationLoader.Garments)
            {
                var defaultName = HasPath(garment.GarmentFile) ? garment.GarmentFile.GetFileNoExt() : "Garment";
                var path = FileDialogs.SaveFile("json", $"{defaultName}.json", "Save Garment As JSON");
                if (null == path)
                {
                    continue;
                }

                garment.WriteJsonFile(new Filename { File = path });
            }
        }

        private void SaveGarments()
        {
            foreach (var garment in AnimationLoader.Garments)
            {
                if (HasPath(garment.GarmentFile))
                {
                    garment.WriteXml();
                }
                else
                {
                    var defaultName = string.IsNullOrEmpty(garment.Name) ? "Garment" : garment.Name;
                    var path = FileDialogs.SaveFile("xml", $"{defaultName}.xml", "Save Garment As");
                    if (null == path)
                    {
                        continue;
                    }

                    garment.WriteXmlFile(new Filename { File = path });
                }
            }
        }

        private static bool HasPath(Filename filename)
        {
            return null != filename && filename.HasFilename;
        }
```

(`Filename`, `Garment`, and `EPlayback` are already in scope via the existing `using AnimationLib;` and `using FilenameBuddy;` at the top of the file — no new `using` statements needed.)

- [ ] **Step 3: Guard `AnimationManager.Update(GameClock, Vector2)` against a blank model**

In `AnimationTool/AnimationManager.cs`, find:

```csharp
        public void Update(GameClock clock, Vector2 position)
        {
            //update the model thing
            SelectedAnimationContainer.Update(clock, position, false, 0.0f, false);
            SelectedAnimationContainer.UpdateRagdoll();
        }
```

Replace with:

```csharp
        public void Update(GameClock clock, Vector2 position)
        {
            if (null == SelectedAnimationContainer.Skeleton.RootBone)
            {
                return;
            }

            //update the model thing
            SelectedAnimationContainer.Update(clock, position, false, 0.0f, false);
            SelectedAnimationContainer.UpdateRagdoll();
        }
```

(`SelectedAnimationContainer.Update(...)` already no-ops safely on a blank model — it checks `CurrentAnimation == null` internally — but `UpdateRagdoll()` doesn't, and dereferences `Skeleton.RootBone` unconditionally. This guard covers every caller of this overload, including `GarmentTab.cs:90` and `TestTab.cs:65`.)

- [ ] **Step 4: Guard `GarmentTab.LoadContent()` against a blank model**

In `AnimationTool/Tabs/GarmentTab.cs`, find (lines 69-71):

```csharp
            var currentAnimation = AnimationManager.SelectedAnimationContainer.CurrentAnimation.Name;
            AnimationManager.SelectedAnimationContainer.SetAnimation(currentAnimation, EPlayback.Loop);
```

Replace with:

```csharp
            if (null != AnimationManager.SelectedAnimationContainer.CurrentAnimation)
            {
                var currentAnimation = AnimationManager.SelectedAnimationContainer.CurrentAnimation.Name;
                AnimationManager.SelectedAnimationContainer.SetAnimation(currentAnimation, EPlayback.Loop);
            }
```

- [ ] **Step 5: Build the project**

Run: `dotnet build AnimationTool/AnimationTool.csproj`
Expected: Build succeeds.

- [ ] **Step 6: Manually verify the blank start doesn't crash**

Run: `dotnet run --project AnimationTool/AnimationTool.csproj`
Expected: The tool launches showing a blank/empty model (no dragon or other character). Click the "Garments" tab button, then the "Test" tab button — neither should crash the app (this exercises the Step 3 and Step 4 guards). Close the app.

- [ ] **Step 7: Commit**

```bash
git add AnimationTool/AnimationManager.cs AnimationTool/Tabs/GarmentTab.cs
git commit -m "$(cat <<'EOF'
Replace hardcoded startup loader with New/Open/Save API

AnimationManager now starts blank and exposes NewModel/OpenModel/
OpenAnimation/NewGarment/OpenGarment/Save/SaveAs/SaveAsJson instead
of calling one hardcoded AnimationsLoader.LoadXxx() method. Adds two
small guards (AnimationManager.Update, GarmentTab.LoadContent) for
assumptions that only held because a character used to always be
preloaded.
EOF
)"
```

---

### Task 3: Wire up File and Garment menus in `ToolsScreen`

**Files:**
- Modify: `AnimationTool/Screens/ToolsScreen.cs`

**Interfaces:**
- Consumes: `AnimationManager.NewModel()`, `.OpenModel()`, `.OpenAnimation()`, `.NewGarment()`, `.OpenGarment()`, `.Save()`, `.SaveAs()`, `.SaveAsJson()` (Task 2).

This adds two `MenuBuddy.Hamburger` dropdown menus ("File" and "Garment") into the existing top toolbar `tabs` `StackLayout`, right after the "Garments" tab button. It removes the old flat Save/SaveJson entries from `AddAsMenuItems()` (superseded by the File menu) and deletes the dead, never-called `AddHamburgerMenu()` method (which would otherwise fail to compile once `Save`/`SaveJson` are renamed).

- [ ] **Step 1: Add the File and Garment menus in `LoadContent()`**

In `AnimationTool/Screens/ToolsScreen.cs`, find:

```csharp
            var garmentTabButton = CreateTabButton("Garments");
            garmentTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new GarmentTab(this, AnimationManager));
            };
            tabs.AddItem(garmentTabButton);

            var testTabButton = CreateTabButton("Test");
```

Replace with:

```csharp
            var garmentTabButton = CreateTabButton("Garments");
            garmentTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new GarmentTab(this, AnimationManager));
            };
            tabs.AddItem(garmentTabButton);

            var fileMenu = new Hamburger(Content.Load<Texture2D>("menu"), true, ScreenManager);
            fileMenu.AddItem(new Label("File", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Bottom,
            });
            fileMenu.AddItem(Content.Load<Texture2D>("menu"), "New", FileNew);
            fileMenu.AddItem(Content.Load<Texture2D>("menu"), "Open Model", FileOpenModel);
            fileMenu.AddItem(Content.Load<Texture2D>("menu"), "Open Animation", FileOpenAnimation);
            fileMenu.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save", FileSave);
            fileMenu.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save As", FileSaveAs);
            fileMenu.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save As JSON", FileSaveAsJson);
            tabs.AddItem(fileMenu);

            var garmentMenu = new Hamburger(Content.Load<Texture2D>("menu"), true, ScreenManager);
            garmentMenu.AddItem(new Label("Garment", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Bottom,
            });
            garmentMenu.AddItem(Content.Load<Texture2D>("menu"), "New", GarmentNew);
            garmentMenu.AddItem(Content.Load<Texture2D>("menu"), "Open", GarmentOpen);
            tabs.AddItem(garmentMenu);

            var testTabButton = CreateTabButton("Test");
```

(`Hamburger` and `Label` are already available via the existing `using MenuBuddy;`; `Content.Load<Texture2D>` and the icon paths follow the exact pattern already used elsewhere in this file. Positioning is handled automatically — `tabs` is a `StackLayout`, which repositions every child it's given regardless of what position the child's own constructor set.)

- [ ] **Step 2: Remove the old Save/SaveJson entries from `AddAsMenuItems()`**

In `AnimationTool/Screens/ToolsScreen.cs`, find:

```csharp
            var hamburgerItems = new List<ContextMenuItem>();
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\save"), "Save", Save));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\save"), "SaveJson", SaveJson));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\undo"), "Undo", Undo));
```

Replace with:

```csharp
            var hamburgerItems = new List<ContextMenuItem>();
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\undo"), "Undo", Undo));
```

- [ ] **Step 3: Delete the dead `AddHamburgerMenu()` method**

In `AnimationTool/Screens/ToolsScreen.cs`, delete this entire method (it's never called anywhere in the codebase today, and would fail to compile once `Save`/`SaveJson` no longer exist under those names):

```csharp
        private void AddHamburgerMenu()
        {
            var hamburger = new Hamburger(Content.Load<Texture2D>("menu"), true, ScreenManager);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save", Save);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\save"), "SaveJson", SaveJson);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\undo"), "Undo", Undo);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\redo"), "Redo", Redo);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\copy"), "Copy", Copy);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\paste"), "Paste", Paste);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\pasteSpecial"), "PasteSpecial", PasteSpecial);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\leftright"), "Mirror", Mirror);
            hamburger.AddItem(Content.Load<Texture2D>(@"icons\unkey"), "UnKey", UnKey);
            AddItem(hamburger);
        }
```

- [ ] **Step 4: Replace the `Save`/`SaveJson` handlers with the new File/Garment handlers**

In `AnimationTool/Screens/ToolsScreen.cs`, in the "Hamburger Event Handlers" region, find:

```csharp
        private void Save(object obj, ClickEventArgs e)
        {
            AnimationManager.Save();
        }

        private void SaveJson(object obj, ClickEventArgs e)
        {
            AnimationManager.SaveJson();
        }
```

Replace with:

```csharp
        private void FileNew(object obj, ClickEventArgs e)
        {
            AnimationManager.NewModel();
            ClearTabsAndScreens();
        }

        private void FileOpenModel(object obj, ClickEventArgs e)
        {
            AnimationManager.OpenModel();
            ClearTabsAndScreens();
        }

        private void FileOpenAnimation(object obj, ClickEventArgs e)
        {
            AnimationManager.OpenAnimation();
            ClearTabsAndScreens();
        }

        private void FileSave(object obj, ClickEventArgs e)
        {
            AnimationManager.Save();
        }

        private void FileSaveAs(object obj, ClickEventArgs e)
        {
            AnimationManager.SaveAs();
        }

        private void FileSaveAsJson(object obj, ClickEventArgs e)
        {
            AnimationManager.SaveAsJson();
        }

        private void GarmentNew(object obj, ClickEventArgs e)
        {
            AnimationManager.NewGarment();
            ClearTabsAndScreens();
        }

        private void GarmentOpen(object obj, ClickEventArgs e)
        {
            AnimationManager.OpenGarment();
            ClearTabsAndScreens();
        }
```

- [ ] **Step 5: Build the project**

Run: `dotnet build AnimationTool/AnimationTool.csproj`
Expected: Build succeeds with no errors (in particular, no leftover references to the removed `Save`/`SaveJson` handler methods).

- [ ] **Step 6: Manually verify the full File/Garment workflow**

Run: `dotnet run --project AnimationTool/AnimationTool.csproj`

Using a real existing asset on disk — e.g. the Grimoire dragon files the old hardcoded `LoadGrimoireDragon()` pointed at (`Spells/Dragon/Dragon_Model.xml` and `Spells/Dragon/Dragon_Animations.xml`, under wherever the sibling `Grimoire/Grimoire.Content/` checkout lives on this machine; any other existing skeleton+animation XML pair on disk works too) — walk through:

1. Confirm the tool launches blank (no character visible).
2. Click the "File" button, click "Open Model", pick a model XML file. Confirm nothing renders yet (no animation loaded).
3. Click "File" > "Open Animation", pick the matching animation XML file. Confirm the character now renders and animates.
4. Move a bone (Model tab) or otherwise change something.
5. Click "File" > "Save". Confirm the file on disk changed (e.g. check its modified timestamp), and that closing/reopening the tool with the same Open Model/Open Animation still shows your change.
6. Click "File" > "New". Confirm the tool goes back to blank.
7. Click "File" > "Open Model" + "Open Animation" again to reload a character.
8. Click "Garment" > "Open", pick an existing garment XML file for that character (e.g. one of the Dragon's `Parts/*/*.xml` files). Confirm it attaches to the skeleton.
9. Click "Garment" > "New". Confirm no crash (an empty, unnamed garment is added).
10. Click "File" > "Save As", choose a new path/filename for the model and then the animation when prompted. Confirm two new files are written.
11. Click "File" > "Save As JSON", choose a path. Confirm a `.json` file is written (in addition to the `.xml` files already saved).

Expected: every step above completes without an exception/crash, and the files written in steps 5, 10, and 11 exist on disk with the expected content.

- [ ] **Step 7: Commit**

```bash
git add AnimationTool/Screens/ToolsScreen.cs
git commit -m "$(cat <<'EOF'
Add File and Garment menus to the toolbar

Adds two Hamburger dropdown menus wired to AnimationManager's new
New/Open/Save API, replacing the old flat Save/SaveJson buttons and
removing the dead AddHamburgerMenu() method.
EOF
)"
```
