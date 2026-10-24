# AGENTS.md — Unity Scripting Guidelines

This document defines the scripting conventions for this project. All agents and contributors must follow these rules when writing or modifying C# scripts.

---

## 1. Keep It Simple

- For small tasks or requirements, **do not over-engineer**. Write short, focused scripts.
- **No tooltips** (`[Tooltip(...)]`) unless explicitly required.
- Use comments only for complex or non-obvious logic — not for self-explanatory lines.
- **Private helper methods** may have a single-line `//` comment above them to briefly describe their purpose.

---

## 2. Variable Declaration Order

All variables must be declared **at the top of the script**, above all functions, in this order:

1. Serialized / public fields
2. Private fields
3. Constants / statics (if any)

```csharp
public class Example : MonoBehaviour
{
    public float Health;
    public int Score;

    [SerializeField] private float _speed;
    private string _name;
    private bool _isDead;
}
```

---

## 3. Naming Conventions

| Access Modifier | Convention | Example |
|---|---|---|
| `public` | PascalCase, starts with capital letter | `public float Health` |
| `private` | Underscore prefix + camelCase | `private string _name` |
| `[SerializeField] private` | Underscore prefix + camelCase | `[SerializeField] private float _speed` |
| Methods | PascalCase | `void TakeDamage()` |
| Local variables | camelCase | `float damageAmount` |

---

## 4. Function Order in Script

Functions must appear in this order:

1. **Unity Event Functions** — `Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`, `OnDisable`, `OnDestroy`, etc.
2. **Public Methods** — user/agent-defined public functions
3. **Private Methods** — user/agent-defined private functions

```csharp
public class Example : MonoBehaviour
{
    // --- Variables ---
    public float Health;
    private bool _isDead;

    // --- Unity Events ---
    void Awake() { }
    void Start() { }
    void Update() { }

    // --- Public Methods ---
    public void TakeDamage(float amount) { }

    // --- Private Methods ---
    private void Die() { }
}
```

---

## 5. General Unity Best Practices

- **Use `[SerializeField]` instead of `public`** when a field only needs to be visible in the Inspector but not accessed externally.
- **Do NOT use `GetComponent<>()` to auto-resolve dependencies** unless explicitly specified. All references should be assigned via the Inspector.
- **Avoid `Find` and `FindObjectOfType`** at runtime. Use Inspector references or dependency injection patterns instead.
- **Avoid excessive null checks and safety guards.** Do not add null checks, `TryGetComponent`, or defensive fallbacks unless the reference is genuinely optional or the task specifically requires it. Trust that Inspector-assigned references are set correctly.
- **Use `CompareTag()`** instead of `== "tagName"` for tag comparisons (avoids string allocation).
- **Prefer `UnityEvent` or C# `Action`/`event`** for decoupled communication over direct cross-script calls where appropriate.
- One script file = one class. File name must match the class name exactly.
- Keep `Update` and `FixedUpdate` lean — move heavy logic into dedicated private methods.
- Use `OnEnable` / `OnDisable` for subscribing and unsubscribing events.
