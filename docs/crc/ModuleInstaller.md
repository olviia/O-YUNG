# CRC — Module Installer
<!-- CLAUDE: card skeleton. You fill the empty parts; I answer under your lines. -->

A CRC card has three parts:
- **Class**: the name.
- **Responsibilities**: what it *knows* and what it *does*. Short verbs. If it's more than about 5, the class does too much.
- **Collaborators**: who it talks to in order to fulfil a responsibility.

---

## Class: `<Module>Installer`  (e.g. InputInstaller)

### Responsibilities
**Knows:**
- Input: both input interfaces? 
<!-- CLAUDE: Close. It knows the module's *parts*: the core (InputModeStack), the adapter
     (InputSystemAdapter) and their settings (mouse sensitivity). It hands out the two interfaces,
     but it isn't the one implementing them. -->

**Does:**
- Input: gives values or raises events on different input
<!-- CLAUDE: That's what the Input *module* does (the adapter, to be exact). The installer
     does something duller, like an electrician, not an appliance:
       - assembles: creates the core, creates the adapter, plugs the adapter into the core
       - hands out the offered ports
       - takes the required ports (Eyes: IGameInput) and passes them to its parts
       - cleans up on shutdown (input.Dispose() moves here)
     Mixing up "what the module does" and "what assembles the module" is the most common
     confusion with this pattern. -->

### Collaborators
- 
<!-- CLAUDE: its own parts (core, adapters), and the root, which calls it. Nothing else.
     An installer never talks to another module's installer. -->


---

## Questions to answer while filling it in
1. Who creates the installer: the root, or is it already in the scene?
i don't know? should it be already on the scene? i am not sure root should create any installers honestly, root should only wire
<!-- CLAUDE: Your instinct is right. The installer must hold inspector references to its
     scene objects, so it has to *live* in the scene, as a MonoBehaviour placed there. The root
     only holds one reference per installer (about 14 total) and wires them. -->
so installers must live on the scene, under some folder named moduleinstallers

2. Lifecycle: which of *create, load, start, stop* does every installer have?
all of them? again, i dont know. maybe only create and remove
<!-- CLAUDE: "Create and remove" is a good minimum (YAGNI: You Aren't Gonna Need It).
     Load/start come back later, when Save exists: Save needs every module created *before*
     anything is loaded into them. Until then: create + remove. -->
wow i was correct!
3. Required ports differ per module (Eyes needs `IGameInput`, Input needs nothing).
   Can all installers share **one** common interface for "build yourself"?
   If not, what *can* they share?
maybe some generic variation? 
<!-- CLAUDE: Good guess, but generics turn ugly here, because each module needs a *different
     set* of ports. The usual answer: "build" is NOT shared. Each installer has its own
     build method with its own parameters (Eyes takes IGameInput, Input takes nothing). That's
     a feature: the signature shows the module's needs at a glance, so nothing is hidden.
     What they can share is the cleanup ("remove"), because it looks the same for everyone. -->
good i dont mind
4. Offered ports: does the root ask for them with a property, a method, or something else?
again i dont know
<!-- CLAUDE: Simplest option: build *returns* what the module offers. Input's build returns its
     two ports; Vision's returns IVision. One call: "here's what you need, give me what you
     offer". Nothing can be read before it's built. -->
okay, but we'll have to teach me how
