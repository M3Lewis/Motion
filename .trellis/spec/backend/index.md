# Backend (Solver & Core Logic) Development Guidelines

> Best practices for solver logic, component data flow, and runtime operations in the Motion plugin.

---

## Overview

This directory contains guidelines for the backend/solver layer of the Motion Grasshopper plugin. It specifies how component data flows, how errors are caught, how resources are disposed of, and how we handle third-party plugin compatibility.

---

## Guidelines Index

| Guide | Description | Status |
|-------|-------------|--------|
| [Directory Structure](./directory-structure.md) | Codebase layout, namespace mappings, and architecture | Active |
| [Data Flow Guidelines](./database-guidelines.md) | SolveInstance, data tree manipulation, parameter retrieval | Active |
| [Error & Resource Management](./error-handling.md) | Exception handling, async task safety, resource disposal | Active |
| [Quality Guidelines](./quality-guidelines.md) | Code quality checklist and naming conventions | Active |
| [Logging Guidelines](./logging-guidelines.md) | Runtime messages, Rhino output, and debug logging | Active |
| [Compatibility Guidelines](./compatibility.md) | Third-party plugin conflict resolution & startup priority | Active |

---

## How to Fill These Guidelines

All guidelines are tailored directly to .NET C# and Rhino Common/Grasshopper SDK development.

**Language**: All documentation is written in **English**.
