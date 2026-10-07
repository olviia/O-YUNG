## Conditions over facts — CLAUDE draft, edit freely (built 2026-10-07)

    Kernel: Condition <- FactCondition<T> <- Bool/Int/Float/String
            ModuleFact<T> (SO: Name, Value, Changed)
    Globals: GlobalFact<T> : ModuleFact<T>  -> static GlobalStore
             GlobalBoolFact / GlobalIntFact (concrete assets)

### CRC: ModuleFact<T> (kernel, Unity side)
Knows: stable name. Does: Value (abstract, from the module's store),
raises Changed (protected RaiseChanged). Used by: FactCondition<T>.

### CRC: FactCondition<T> (kernel, abstract)
Knows: one ModuleFact<T>. Does: Changed passes to fact.Changed;
gives subclasses Value. Pattern: Template Method.

### CRC: GlobalFact<T> (Globals)
Knows: its GlobalStore (private, given by installer). Does: Value =
store.Get<T>(Name), throws if unbound; Bind subscribes RaiseChanged
by Name (explicit IGlobalFact impl).

### CRC: GlobalCatalog (SO) / GlobalsInstaller (MonoBehaviour)
Catalog knows every fact (List<ScriptableObject> as IGlobalFact),
fills itself in editor, logs duplicate names. Installer: new store,
Bind every catalog fact; Dispose unbinds. Called by CompositionRoot.

## Next
- Save / Yarn lookup by name through the catalog
- GlobalStringFact / GlobalFloatFact when a caller needs them
