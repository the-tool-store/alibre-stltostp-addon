# alibre-stltostp-addon — Code Review (Correctness)

**Date:** 2026-06-20
**Scope:** Second-opinion review, code only (correctness bugs). Only one reviewable source file (`source/AlibreAddOn.cs`); `stltostp.exe` is a prebuilt binary.

**Summary: 1 bug — 1 Medium**

## Medium

**`source/AlibreAddOn.cs:124-126`** — Synchronous `StandardOutput.ReadToEnd()` followed by `StandardError.ReadToEnd()` before `WaitForExit()` can deadlock the addon.

Why it's wrong: Both stdout and stderr are redirected (lines 115-116). The code blocks on `proc.StandardOutput.ReadToEnd()` (line 124) and only reads stderr afterward. If the converter process writes enough to stderr to fill the OS pipe buffer (~4 KB on Windows) while the parent is still blocked reading stdout, the child blocks on its stderr write and the parent blocks on its stdout read — a classic mutual deadlock that hangs the Alibre UI thread indefinitely. Microsoft's `ProcessStartInfo` docs warn against reading both redirected streams synchronously; one stream should be read asynchronously (e.g. `BeginOutputReadLine` / reading one stream on a separate task) or read after the other completes via async. Given `stltostp.exe` could emit verbose diagnostics to stderr on a bad/large STL, this is a realistic hang.

## Notes on items checked and deemed NOT bugs

- Lines 45-48: null-conditional access returning `null` for unknown menu IDs is intentional/defensive, not a bug.
- Line 46 `SubMenuItems` returning `null` when the item is missing is consistent with the other accessors and is reached only for registered IDs in practice.
- Line 118 `using var proc` correctly disposes the process; exception paths are covered by the outer try/catch (lines 109-165).
- The `NotImplementedException` overloads (lines 63-70) are explicit-interface stubs for an overload the host does not call in this configuration; not a runtime bug in the exercised path.

No High-severity correctness bugs were found.

---

## Fixes applied — 2026-06-20

- **[Medium] `source/AlibreAddOn.cs`** — stderr is now drained concurrently via `StandardError.ReadToEndAsync()` while stdout is read synchronously (then `stdErrorTask.Result` + `WaitForExit()`), eliminating the pipe-buffer deadlock. Added `using System.Threading.Tasks;`. Captured stdout/stderr strings remain available to the downstream code.

*Caveat: change applied to source; not verified by build.*
