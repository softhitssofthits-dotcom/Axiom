# Axiom

[Русский](README.md) · [English](README.en.md)

<p align="center">
  <img src="ide/src/renderer/assets/logo-light.png" width="72" alt="Axiom logo">
</p>

<p align="center"><strong>BASIC-inspired syntax, strict types, and a dedicated development environment.</strong></p>

![version](https://img.shields.io/badge/version-0.12.0-blue)
![IDE](https://img.shields.io/badge/IDE-0.15.3-green)
![platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![status](https://img.shields.io/badge/status-early%20preview-orange)
![license](https://img.shields.io/badge/license-MIT-blue)

<p align="center">
  <a href="https://softhitssofthits-dotcom.github.io/Axiom/">📖 Language reference</a> ·
  <a href="#quick-start">🚀 Quick start</a> ·
  <a href="samples/">🧩 Code examples</a> ·
  <a href="https://github.com/softhitssofthits-dotcom/Axiom/issues">💬 Discussions and bug reports</a>
</p>

**Axiom** is a programming language in the BASIC family, with its own desktop IDE. Start with a small console program, move on to arrays and user-defined types, then build a graphical program or a small game—all in one environment with the reference and debugger close at hand.

Programs use the `.axm` file extension. The compiler translates each program into bytecode, which runs on the Axiom virtual machine.

[![Axiom IDE: TYPE records, array sorting, and program output](assets/readme/axiom-ide.png)](assets/readme/axiom-ide.png)

*A real screenshot of Axiom IDE: the program sorts a fruit basket by price. [Open this example](samples/smoke/readme/fruit_basket.axm).*

## Familiar syntax, modern features

A small program can be read from top to bottom:

```basic
DIM fruits[] AS STRING = ["apple", "banana", "orange"]

FOR EACH fruit IN fruits[]
    PRINT $"In the basket: {fruit}"
NEXT fruit

PRINT $"Total fruit: {ARRAYLENGTH(fruits[])}"
```

```text
In the basket: apple
In the basket: banana
In the basket: orange
Total fruit: 3
```

Square brackets denote arrays; parentheses denote function calls. String interpolation inserts values into text, and `FOR EACH` visits each element without manual index management.

| Area | Axiom features |
|---|---|
| **Types and numbers** | Strict typing, `BOOLEAN`, `INTEGER`, `LONG`, `DOUBLE`, `DECIMAL`, strings, dates, times, and UTC values |
| **Arrays and collections** | `[...]` literals, multidimensional arrays, custom bounds, views, `REDIM PRESERVE`, record sorting, search, filtering, and aggregation; typed `DICTIONARY(key, value)` with `Add`, `Exists`, `Remove`, `Keys`, `Count`, and `TryGetValue`; typed `HASHSET(element type)` with `Add`, `Contains`, `Remove`, `Count`, `ToArray`, `UnionWith`, and `IntersectWith` |
| **User-defined types** | `TYPE` records with fields, methods, and constructors; record literals and structural equality |
| **Functions and modules** | `SUB`, `FUNCTION`, `BYVAL`/`BYREF`, nested routines, functions as values, closures, `IMPORT`, `PUBLIC`, and `PRIVATE` |
| **Strings and errors** | `$"..."` interpolation, Unicode utilities, regular expressions, `TRY` / `CATCH` / `FINALLY` |
| **Files and time** | Text input/output, directories and paths, date, time, and time-zone functions |
| **Graphics and events** | Classic `SCREEN`, modern `CANVAS`, images, sprites, Tiled maps, keyboard, and mouse |

The language reference currently lists **282 built-in functions and 355 calling forms**. See the [language reference](https://softhitssofthits-dotcom.github.io/Axiom/) for each form's behavior and limitations.

## IDE: from the first line to debugging

The editor is built on Monaco, and the console uses xterm.js. A .NET service connects the editor to the compiler and virtual machine.

| Workflow | IDE tools |
|---|---|
| **Write** | Syntax highlighting, code completion, type hints, and diagnostics as you type |
| **Navigate** | Program outline, routine navigation, and caller/callee relationships |
| **Run** | Built-in console, `INPUT`, run and stop controls, and separate graphics windows |
| **Debug** | Breakpoints, step-by-step tracing, step into, step out, and step back |
| **Organize** | Multiple files, `.axproj` projects, and a project tree for source and resources |
| **Customize** | Four themes, two code styles, and Russian or English interface |
| **Protect work** | External-change conflict checks and recovery of completed edits after a crash |

### Tracing and debugging

Pause on a line and follow the program one step at a time. Step into a function, run a call, return to its caller, or step back. The current line is highlighted; when iterating over an array, the status bar also shows the current element.

[![Axiom debugging: breakpoint, current line, and array element](assets/readme/axiom-debugger.png)](assets/readme/axiom-debugger.png)

*Paused before printing the next fruit, with the breakpoint, current instruction, and step controls visible.*

### Classic and Modern: two styles for one language

Prefer the uppercase keywords of classic BASIC, or a calmer title-case style? The formatter supports both:

| Classic | Modern |
|---|---|
| `DIM fruits[] AS STRING` | `Dim fruits[] As String` |
| `FOR EACH fruit IN fruits[]` | `For Each fruit In fruits[]` |
| `PRINT fruit` | `Print fruit` |
| `NEXT fruit` | `Next fruit` |

The program behaves the same; only the presentation of keywords, types, and built-in functions changes. The code style is independent of the IDE theme.

<details>
<summary><strong>See the same program in Modern style</strong></summary>

![Axiom IDE showing the same program in Modern style](assets/readme/axiom-modern.png)

</details>

IDE themes: **Light**, **Dark**, **QuickBASIC**, and **Borland Pascal**. The screenshots use the Dark theme.

## Explore the examples

| Program or topic | What to see |
|---|---|
| [Fruit basket](samples/smoke/readme/fruit_basket.axm) | The program from the screenshot: `TYPE`, sorting by field, `FOR EACH`, and interpolation |
| [Array literals](samples/expressions/array-literals/) | Concise value lists and how element types are inferred |
| [Language operators](samples/operators/) | Short examples of individual language constructs |
| [Built-in functions](samples/functions/) | Programs organized by function name |
| [Graphics](samples/graphics/) | Drawing, images, sprites, and tile maps |
| [Maze Chase](samples/graphics/game/maze_chase_project/README.md) | A multi-file game project with resources and `.axproj` details |

## Quick start

Running from source requires **Windows**, **.NET SDK 10**, and **Node.js with npm**.

```powershell
git clone https://github.com/softhitssofthits-dotcom/Axiom.git
cd Axiom
.\run-ide.bat
```

The launcher checks the required tools, builds the needed components, and installs IDE dependencies. Internet access is required on the first run to download packages.

Open the [fruit basket example](samples/smoke/readme/fruit_basket.axm) or another `.axm` file in the IDE, select **Check**, then **Run**. Multi-file programs open through an `.axproj` file.

## Why Axiom exists

I enjoyed BASIC at school. Years later, I still wanted to write in a familiar, clear syntax. Axiom is my open-source project, bringing that idea together with modern types, graphics, and development tools.

One author works on the project with AI agents: the author sets the direction and language rules, while agents help with implementation, checks, and documentation. I enjoy both the language and this way of building it.

If you like BASIC, I welcome feedback and suggestions. A bug report, a small example, or a discussion about a design choice can help the project grow. [Open an issue →](https://github.com/softhitssofthits-dotcom/Axiom/issues)

## Documentation and source

- **[Language reference online](https://softhitssofthits-dotcom.github.io/Axiom/)** — syntax, rules, examples, and topic search.
- [Local copy of the reference](docs/manual/index.html).

| Directory | Contents |
|---|---|
| `src/` | Compiler, bytecode, virtual machine, and .NET services |
| `ide/` | Electron interface, Monaco editor, and console |
| `samples/` | Runnable examples organized by topic |
| `docs/manual/` | Offline language reference |

## Project status

Axiom is in early development. Current versions: language `0.12.0`, compiler `0.13.15`, IDE `0.15.3`. Windows is the primary platform. The language and IDE are still evolving; a portable distribution and support for other platforms have not been announced.

## License

[MIT](LICENSE). Thanks to the BASIC community and to the authors of .NET, Electron, Monaco, and xterm.js, which power Axiom.
