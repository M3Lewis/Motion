# State Synchronization Guidelines

> Managing slider timeline sync, event state tracking, and controlled component lock/hide states.

---

## 1. Timeline Synchronization

Motion uses a Single Controller model where the `Motion Slider` globally broadcasts time frames.
- **Smart Connection**: Senders and solver operations automatically wire themselves to the nearest/active `Motion Slider`.
- **Auto-Sync Bounds**: If a downstream `Motion Sender` defines a time domain exceeding the current slider range, the slider must dynamically expand its minimum/maximum limits.
- Call `slider.Slider.Maximum = Math.Max(slider.Slider.Maximum, senderLimit)` and expire the solution.

---

## 2. Event Group States

- **Group Locking**: When the current timeline frame falls outside a component's event time range, all controlled components within its registered `GH_Group` must be locked (`component.Locked = true`).
- **Group Hiding**: Similarly, components outside the active range should be hidden on the canvas (`component.Hidden = true`).
- Maintain bidirectional navigation links between event definitions and controlled components.

---

## 3. Propagation Safety

- Do not trigger infinite solver expiration loops. When modifying another component's state (e.g. updating a slider value or changing its bounds), ensure you check if the target state has already changed before expiring the document solution.
- Use `ExpireSolution(false)` or schedule solution recalculation safely via `GH_Document.ScheduleSolution`.
