# Code Review — alibre-stltostp-addon

- **Date:** 2026-06-15
- **Branch:** `review/2026-06-15-code-review` (branched from `the-tool-store` @ `dce0fab` "cleanup push")
- **Reviewer:** Claude (Opus 4.8)
- **Scope:** Full repository review (C# add-on host + bundled native converter `stltostp.exe` + project/build files)

---

## 1. Summary

This is an Alibre Design add-on that adds a ribbon menu item ("Import STL File") which prompts
the user for an `.stl` file, shells out to a bundled native console converter (`stltostp.exe`) to
produce a `.stp` STEP file next to the source, and then imports that STEP file back into the
active Alibre session. The single C# file `source/AlibreAddOn.cs` is the entire COM-facing host;
all the actual geometry conversion lives in the opaque `stltostp.exe` binary.

The repo is very small — 6 tracked source files (plus a 100 KB committed binary), ~200 LOC of
real C#. The menu/command plumbing and the process-launch logic are reasonable and well
guarded with error dialogs. However, the project file is heavily polluted with **copy-paste cruft
from at least two other add-ons** ("Assimp" namespace, IronPython packages that are never used,
a phantom `config.json`), there are **hard-coded version-pinned Alibre paths that even disagree
with each other between files**, the **`.adc` manifest points the icon at a folder that does not
exist**, a **100 KB prebuilt `.exe` is committed to source control**, and the duplicate
throwing `SaveData`/`LoadData` overloads from the sibling repo are present here too.

**Overall:** A small, mostly-working add-on whose runtime logic is sound, but whose project file
and packaging are full of inherited cruft and portability landmines. No installer or CI exists in
this repo (unlike the sibling), so the "broken installer" class of bug does not apply here. Clean
up the project file and the manifest before shipping.

### Findings by severity

| Severity | Count |
|----------|-------|
| Critical | 0 |
| High     | 4 |
| Medium   | 4 |
| Low / Nit| 6 |

---

## 2. Critical

None. There is no installer build path or required-but-missing runtime script in this repo, so
the two Critical findings from the sibling review do not have analogues here. The add-on, once
built, has all of its runtime dependencies (`stltostp.exe`, `logo.ico`, the `.adc`) copied to the
output directory.

---

## 3. High

### H-1. Hard-coded, version-pinned Alibre paths — and they disagree with each other
**Files:** [alibre-stltostp-addon.csproj:44,48](source/alibre-stltostp-addon.csproj), [Properties/launchSettings.json:8](source/Properties/launchSettings.json)

The reference assemblies are pinned to one exact Alibre build:

```xml
<HintPath>C:\Program Files\Alibre Design 28.1.0.28223\Program\AlibreAddOn.dll</HintPath>
<HintPath>C:\Program Files\Alibre Design 28.1.0.28223\Program\AlibreX.dll</HintPath>
```

while the debug-launch profile points at a *different* version:

```json
"executablePath": "C:\\Program Files\\Alibre Design 28.1.1.28227\\Program\\Alibre Design.exe"
```

So the project references `28.1.0.28223` but launches `28.1.1.28227`. On any machine that does not
have *both* of these exact builds installed, the build fails (missing `HintPath`) and/or F5 debug
fails (missing exe). Resolve the install root from the registry key the installer writes
(`SOFTWARE\Alibre, LLC\Alibre Design`) or an MSBuild property / env var, and use a single source of
truth for the version. At minimum, make the two files agree.

### H-2. Unused IronPython package + import dependencies bloat the add-on
**Files:** [alibre-stltostp-addon.csproj:38-41](source/alibre-stltostp-addon.csproj), [AlibreAddOn.cs:3-4](source/AlibreAddOn.cs)

The project pulls in two sizable NuGet packages:

```xml
<PackageReference Include="IronPython" Version="3.4.2" />
<PackageReference Include="IronPython.StdLib" Version="3.4.2" />
```

and the code `using`s them:

```csharp
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
```

but **nothing in `AlibreAddOn.cs` ever references IronPython, the scripting engine, or runs any
Python** — the converter is the external `stltostp.exe`. These are leftovers from the sibling
"shapes" add-on (which does run IronPython). They drag the IronPython runtime + StdLib (tens of
MB) into the build output for no reason, increasing the deployed footprint and the dependency /
supply-chain surface. Remove both `PackageReference`s and the two `using` directives.

### H-3. 100 KB prebuilt `stltostp.exe` committed to source control
**File:** `source/stltostp.exe` (tracked; PE32+ x86-64 console binary, 102,400 bytes)

```
$ git ls-files | grep exe
source/stltostp.exe
```

The core conversion logic ships as an opaque committed binary with **no source, no build script,
and no provenance** in this repo. This is a trust/supply-chain hazard (a reviewer cannot audit what
the add-on actually executes against the user's files), bloats git history on every rebuild, and is
exactly the kind of artifact a `.gitignore` is meant to keep out. Either vendor the converter's
source (or add it as a submodule — note `submodules/` is empty) and build it as part of the
solution, or fetch a pinned, checksummed release at build time. If it must stay, document its
origin and a hash.

### H-4. Duplicated `SaveData`/`LoadData` with one pair throwing `NotImplementedException`
**File:** [AlibreAddOn.cs:59-70](source/AlibreAddOn.cs)

`AddOnRibbon` declares two overloads each of `SaveData`/`LoadData`: a no-op pair typed against
`System.Runtime.InteropServices.ComTypes.IStream` (the `using` alias at [line 13](source/AlibreAddOn.cs))
and a second pair typed against `global::AlibreAddOn.IStream` that `throw new NotImplementedException()`:

```csharp
public void SaveData(IStream pCustomData, string sessionIdentifier) { }            // no-op
public void LoadData(IStream pCustomData, string sessionIdentifier) { }            // no-op
...
public void LoadData(global::AlibreAddOn.IStream pCustomData, string sessionIdentifier)
{
    throw new NotImplementedException();
}
public void SaveData(global::AlibreAddOn.IStream pCustomData, string sessionIdentifier)
{
    throw new NotImplementedException();
}
```

The `IAlibreAddOn` interface is satisfied by exactly one of these signatures — almost certainly the
`global::AlibreAddOn.IStream` one (the SDK type). That means the methods the host actually calls are
the **throwing** ones. `HasPersistentDataToSave` returns `false`, which guards `SaveData`, but
`LoadData` can be invoked on document open regardless, and an unhandled `NotImplementedException`
crossing the COM boundary can destabilize the host. Confirm which overload the interface binds and
make the bound implementation a safe no-op (delete the throwing pair or invert which is the no-op).

---

## 4. Medium

### M-1. `RootNamespace` is leftover from an unrelated "Assimp" project
**File:** [alibre-stltostp-addon.csproj:6](source/alibre-stltostp-addon.csproj)

```xml
<RootNamespace>AssimpInsideAlibreDesignAddon</RootNamespace>
```

The actual namespace in code is `AlibreAddOnAssembly` ([AlibreAddOn.cs:15](source/AlibreAddOn.cs)).
This `RootNamespace` is copy-paste cruft from a third, unrelated add-on. It is harmless to the COM
registration (which uses the real namespaced type names) but actively misleads anyone reading the
project. Set it to `AlibreAddOnAssembly` or remove it.

### M-2. `.csproj` copies a `config.json` that does not exist
**File:** [alibre-stltostp-addon.csproj:56-58](source/alibre-stltostp-addon.csproj)

```xml
<None Update="config.json">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</None>
```

There is no `config.json` anywhere in the repo (confirmed: `source/config.json` does not exist).
`<None Update>` on a non-existent file is silently ignored by MSBuild, so it is not fatal, but it is
dead configuration carried over from another add-on. Remove it (or add the file if the converter is
meant to read one).

### M-3. `.adc` manifest icon path points at a non-existent `Icons\` folder
**File:** [alibre-stltostp-addon.adc:5](source/alibre-stltostp-addon.adc)

```xml
<Icon location="Icons\logo.ico"/>
```

The icon actually lives at `source/logo.ico` and is copied to the **output root** by the csproj
([csproj:28-30](source/alibre-stltostp-addon.csproj)), not into an `Icons\` subfolder (there is no
`Icons\` directory in the repo). At runtime Alibre will look for `Icons\logo.ico` relative to the
add-on and fail to find it, so the ribbon item shows no icon. Either change the manifest to
`location="logo.ico"` or copy the icon into `Icons\` in the build output and reference it
consistently. (Note the in-code `MenuIcon` also always returns `null` — see L-1 — so icons are
doubly disabled.)

### M-4. Stale `Remove` block references a directory that does not exist; `AlibreAddOn.cs` listed as `<None>`
**File:** [alibre-stltostp-addon.csproj:17-22,36](source/alibre-stltostp-addon.csproj)

```xml
<Compile Remove="AlibreAddOn - Copy\**" />
<EmbeddedResource Remove="AlibreAddOn - Copy\**" />
<None Remove="AlibreAddOn - Copy\**" />
<Page Remove="AlibreAddOn - Copy\**" />
...
<None Include="AlibreAddOn.cs" />
```

`AlibreAddOn - Copy\**` does not exist — copy-paste leftover. More notably, `AlibreAddOn.cs` is
explicitly added as `<None>`. Under the SDK-style project, `.cs` files are auto-included as
`<Compile>` by the default globs, so this `<None Include>` is redundant (the file still compiles via
the implicit glob) — but it is confusing and, if anyone ever sets `<EnableDefaultCompileItems>false</...>`,
it would silently turn the only source file into a non-compiled item. Remove the dead `Remove` block
and drop the `<None Include="AlibreAddOn.cs">`.

---

## 5. Low / Nits

### L-1. `MenuItem` constructor silently ignores its `icon` parameter
[AlibreAddOn.cs:80-86](source/AlibreAddOn.cs): the ctor takes `string icon = null` and callers pass
`"logo.ico"` ([lines 175,181](source/AlibreAddOn.cs)), but the body hard-codes `Icon = null;` with a
comment "Icon functionality is disabled". Combined with `MenuIcon` always returning `null`
([line 49](source/AlibreAddOn.cs)) and the `Icon` property never being read, all icon plumbing is
dead code. Either implement it (return `Icon` from `MenuIcon`) or remove the parameter/property.

### L-2. Class named identically to a namespace forces `global::` qualifiers
`public static class AlibreAddOn` ([line 17](source/AlibreAddOn.cs)) inside `namespace
AlibreAddOnAssembly` collides with the SDK's `AlibreAddOn` namespace (`using AlibreAddOn;` at
[line 1](source/AlibreAddOn.cs)), which is exactly why `global::AlibreAddOn.IStream` is needed at
lines 63/67. Renaming the static class (e.g. `AddOnEntryPoint`) removes the ambiguity.

### L-3. `InvokeCommand` does not null-check the session lookup
[AlibreAddOn.cs:50-55](source/AlibreAddOn.cs): `_alibreRoot.Sessions.Item(sessionIdentifier)` can
return null / throw for an unknown identifier, and the result is passed straight into the command.
The command (`RunCmd`) does not actually use the `session` parameter (it imports via the global root
instead — see L-4), so the risk is low today, but guard the lookup to avoid an NRE/exception crossing
the COM boundary.

### L-4. `RunCmd` ignores the `session` it is handed and uses the global root
[AlibreAddOn.cs:88,134-137](source/AlibreAddOn.cs): `RunCmd(IADSession session)` never uses
`session`; it imports the STEP via `AlibreAddOn.GetRoot().ImportSTEPFile(stepPath)`. If multiple
sessions/documents are open, the import may land in a different document than the one the menu
command was invoked from. Prefer importing into the passed-in `session` if the API allows.

### L-5. `SubMenuItems` returns `null` instead of an empty array
[AlibreAddOn.cs:46](source/AlibreAddOn.cs):
`_menuManager.GetMenuItemById(menuID)?.SubItems.Select(...).ToArray()` returns `null` when the id is
unknown (the `?.` short-circuits). COM hosts generally expect a non-null `Array`; return
`Array.Empty<int>()` on the miss to be safe.

### L-6. STEP output path collides with / overwrites any existing `.stp` silently
[AlibreAddOn.cs:100](source/AlibreAddOn.cs): `Path.ChangeExtension(stlPath, ".stp")` writes next to
the source STL and will silently overwrite an existing `foo.stp` without warning, and requires write
permission in the STL's directory (may fail for files under `Program Files` or read-only shares).
Consider writing to a temp dir or prompting for the output location / confirming overwrite.

---

## 6. What looks good

- Clean separation: the C# host is a thin shell-out + import wrapper; the heavy lifting is isolated
  in the external converter.
- The process launch is done correctly: `UseShellExecute = false`, `CreateNoWindow = true`, stdout
  and stderr both redirected and surfaced on failure, `WaitForExit()`, and exit-code + output-file
  existence both checked before declaring success ([AlibreAddOn.cs:111-160](source/AlibreAddOn.cs)).
- The converter path is resolved relative to the executing assembly
  ([line 101-102](source/AlibreAddOn.cs)) rather than hard-coded — good, portable.
- Error handling is thorough: missing-exe, failed-start, non-zero-exit, and import-failure each get a
  specific, actionable `MessageBox`.
- Lifecycle (`AddOnLoad`/`AddOnUnload`) nulls out the statics on unload
  ([lines 26-30](source/AlibreAddOn.cs)).
- The runtime assets (`stltostp.exe`, `logo.ico`, `.adc`) all have `CopyToOutputDirectory` entries, so
  the built add-on is self-contained (contrast with the sibling repo's missing-script bug).

---

## 7. Recommended fix order

1. **H-1** — unpin the Alibre version and reconcile the two disagreeing paths so the project builds
   and debugs on a normal install.
2. **H-2** — drop the unused IronPython packages/usings (large, easy footprint + supply-surface win).
3. **H-4** — verify and fix the bound `SaveData`/`LoadData` overload so the host cannot hit a thrown
   `NotImplementedException`.
4. **H-3** — establish provenance for `stltostp.exe` (build from source/submodule, or pin+checksum)
   and keep the binary out of git.
5. **M-1 / M-2 / M-3 / M-4** — sweep the project-file and manifest cruft (Assimp namespace, phantom
   `config.json`, `Icons\` icon path, dead `Remove` block / `<None Include>`).
6. **L-*** — icon plumbing, session handling, null-safety, and output-path UX when next touching the
   code.
