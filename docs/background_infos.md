# Computation Expressions in F# — A Tutorial

## Table of Contents

1. [What is a Computation Expression?](#1-what-is-a-computation-expression)
2. [The Core Methods](#2-the-core-methods)
3. [Custom `result { }` Builder](#3-custom-result--builder)
4. [Custom `taskResult { }` Builder](#4-custom-taskresult--builder)
5. [Standard Computation Expressions](#5-standard-computation-expressions)
   - [seq { }](#seq--)
   - [async { }](#async--)
   - [task { }](#task--)
6. [Deep Dive: task { }](#6-deep-dive-task--)
   - [How task { } Works](#61-how-task--works)
   - [task vs async](#62-task-vs-async)
   - [Cancellation and SynchronizationContext](#63-cancellation-and-synchronizationcontext)
   - [Real-World Patterns](#64-real-world-patterns)
7. [Rising Complexity Examples](#7-rising-complexity-examples)
   - [Level 1: Simple Wrapping](#level-1-simple-wrapping)
   - [Level 2: Sequencing](#level-2-sequencing)
   - [Level 3: Error Handling with Result](#level-3-error-handling-with-result)
   - [Level 4: Async + Result Combined](#level-4-async--result-combined)
   - [Level 5: Nested Computation Expressions](#level-5-nested-computation-expressions)
8. [Builder Method Reference](#8-builder-method-reference)

---

## 1. What is a Computation Expression?

A **computation expression** (CE) is an F# syntactic sugar for writing monadic or builder-based code in a clean, imperative-looking style. Instead of manually threading state, handling errors, or chaining callbacks, you write code that looks sequential while the builder handles the plumbing.

The syntax is:

```fsharp
builder-name {
    <expressions>
}
```

Every CE is backed by a **builder class** with special methods (`Bind`, `Return`, `Combine`, etc.). The F# compiler desugars the `{ }` block into calls to those methods.

### Motivation

Consider three functions that each take an input, validate or transform it, and return `Result`:

```fsharp
let step1 (x: int) : Result<int, string> =
    if x >= 0 then Ok (x + 1)
    else Error "input must be non-negative"

let step2 (a: int) : Result<int, string> =
    if a % 2 = 0 then Ok (a * 2)
    else Error "value must be even"

let step3 (b: int) : Result<int, string> =
    if b > 5 then Ok (b * 3)
    else Error "value must be greater than 5"
```

Chaining them manually — passing each `Ok` value into the next function, short-circuiting on `Error` — looks like this:

```fsharp
let process x =
    match step1 x with
    | Error e -> Error e
    | Ok a ->
        match step2 a with
        | Error e -> Error e
        | Ok b ->
            match step3 b with
            | Error e -> Error e
            | Ok c -> Ok (a + b + c)
```

Using `Result.bind` reduces the nesting but is still hard to read:

```fsharp
let process x =
    Result.bind (fun a ->
        Result.bind (fun b ->
            Result.bind (fun c ->
                Ok (a + b + c)
            ) (step3 b)
        ) (step2 a)
    ) (step1 x)
```

Each step feeds its output into the next: `step1 -> step2 -> step3`. But the deep nesting makes it easy to accidentally pass the wrong variable or lose track of which value is in scope. The CE eliminates both the nesting and the risk:

```fsharp
let process x = result {
    let! a = step1 x
    let! b = step2 a
    let! c = step3 b
    return a + b + c
}
```

In Haskell, the same monadic pattern uses `do` notation with `Either`:

```haskell
-- Haskell: Either as a monad with do notation
process :: Int -> Either String Int
process x = do
    a <- step1 x       -- step1 :: Int -> Either String Int
    b <- step2 a       -- step2 :: Int -> Either String Int
    c <- step3 b       -- step3 :: Int -> Either String Int
    return (a + b + c)

-- Without do notation (>>= bind operator):
process x =
    step1 x >>= \a ->
    step2 a >>= \b ->
    step3 b >>= \c ->
    return (a + b + c)
```

The mapping is direct:

| F# | Haskell | Role |
|----|---------|------|
| `Result<'a, 'e>` | `Either e a` | Error-aware return type |
| `Result.bind` | `>>=` | Bind operator |
| `result { let! x = e }` | `do { x <- e }` | Unwrap and continue |
| `return x` | `return x` | Wrap in success |
| `Result.map` | `fmap` / `<$>` | Transform success value |

`do` notation in Haskell is the language's built-in sugar for any monad — you don't need a custom builder, every monad works automatically. F# computation expressions achieve the same ergonomics but require an explicit builder class per monad (like `ResultBuilder`), giving you the flexibility to customize behavior per CE.

---

## 2. The Core Methods

Every builder can define any subset of these methods. The compiler desugars CE syntax into calls to them.

| Method | F# Signature | F# Syntax | Purpose | Haskell Equivalent |
|--------|-------------|-----------|---------|-------------------|
| `Bind` | `M<'a> -> ('a -> M<'b>) -> M<'b>` | `let! x = expr` | Unwrap, bind to name, continue | `>>=` bind operator, or `<-` in `do` notation |
| `Return` | `'a -> M<'a>` | `return x` | Wrap a value into the monad | `return x` (same keyword) |
| `ReturnFrom` | `M<'a> -> M<'a>` | `return! expr` | Return an already-wrapped value | Plain `expr` as last expression in `do` block |
| `Zero` | `unit -> M<'a>` | (implicit) | Default for empty branches | `mzero` from `MonadPlus`, or `guard` |
| `Combine` | `M<'a> -> M<'a> -> M<'a>` | `expr1; expr2` | Sequence two expressions | `>>` operator (>>= ignoring the value) |
| `Delay` | `(unit -> M<'a>) -> M<'a>` | (implicit) | Wrap a lazy computation | Not needed (Haskell is lazy by default) |
| `For` | `seq<'a> -> ('a -> M<'b>) -> M<'b>` | `for x in xs` | Loop over sequence | `forM_` / `mapM_` :: `[a] -> (a -> m b) -> m ()` |
| `While` | `(unit -> bool) -> M<'a> -> M<'a>` | `while cond` | Conditional loop | Recursive helper function (no built-in while) |
| `TryWith` | `M<'a> -> (exn -> M<'a>) -> M<'a>` | `try ... with` | Exception handling | `catch` from `Control.Exception` |
| `TryFinally` | `M<'a> -> (unit -> unit) -> M<'a>` | `try ... finally` | Cleanup | `finally` or `bracket` from `Control.Exception` |
| `Using` | `'a -> ('a -> M<'b>) -> M<'b>` | `use x = expr` | IDisposable binding | `bracket` pattern: `withFile`, `withResource` |

### What `let!` Desugars To

```fsharp
builder {
    let! x = expr
    body
}
// becomes:
builder.Bind(expr, fun x -> builder { body })
```

### What `return` Desugars To

```fsharp
builder { return x }
// becomes:
builder.Return(x)
```

### What `return!` Desugars To

```fsharp
builder { return! expr }
// becomes:
builder.ReturnFrom(expr)
```

---

## 3. Custom `result { }` Builder

The Winslow codebase defines its own `result { }` builder because F# 10 removed the built-in one.

```fsharp
type ResultBuilder() =
    member _.Bind(m, f)   = Result.bind f m
    member _.Return(x)    = Ok x
    member _.ReturnFrom(x) = x
    member _.Zero()       = Ok ()

let result = ResultBuilder()
```

### How It Works

- **Bind**: Takes `Result<'a, 'e>` and a function `'a -> Result<'b, 'e>`, applies the function if `Ok`, short-circuits if `Error`.
- **Return**: Wraps any value in `Ok`.
- **ReturnFrom**: Passes through an already-wrapped `Result`.
- **Zero**: Provides `Ok ()` for empty else branches.

### Usage Example

```fsharp
type DomainError =
    | NotFound of string
    | ValidationError of string * string

let validateName (s: string) =
    if String.IsNullOrWhiteSpace s then
        Error (ValidationError("name", "must not be empty"))
    else
        Ok s

let validateAge (age: int) =
    if age < 0 then
        Error (ValidationError("age", "must be positive"))
    else
        Ok age

let createPerson name age = result {
    let! validatedName = validateName name
    let! validatedAge  = validateAge age
    return { Name = validatedName; Age = validatedAge }
}

// Usage:
// Ok { Name = "Alice"; Age = 30 }
// Error (ValidationError ("name", "must not be empty"))
```

### Error Short-Circuiting

The key behavior: when any `let!` hits an `Error`, the rest of the block is skipped and the error propagates. This is what makes railway-oriented programming possible.

```fsharp
let example = result {
    let! a = Ok 1          // Ok -> continues
    let! b = Error "fail"  // Error -> exits block
    let! c = Ok 3          // never reached
    return a + b + c
}
// Result: Error "fail"
```

---

## 4. Custom `taskResult { }` Builder

The Winslow codebase defines `taskResult { }` for async operations that return `Task<Result<_, _>>`.

```fsharp
open System.Threading.Tasks

type TaskResultBuilder() =
    member _.Return(x) = task { return Ok x }
    member _.ReturnFrom(x) = x
    member _.Bind(m: Task<Result<'a, 'e>>, f: 'a -> Task<Result<'b, 'e>>) =
        task {
            let! r = m
            match r with
            | Ok v    -> return! f v
            | Error e -> return Error e
        }
    member _.Zero() = task { return Ok () }

let taskResult = TaskResultBuilder()
```

### How It Works

- **Bind**: Awaits the `Task`, pattern matches the `Result`. If `Ok`, calls the continuation. If `Error`, wraps it back in a `Task<Result<_, _>>` and short-circuits.
- **Return**: Wraps a value as `Ok` inside a `Task`.
- **ReturnFrom**: Passes through an already-compatible `Task<Result<_, _>>`.
- **Zero**: Returns `Ok ()` inside a `Task`.

### Usage

```fsharp
let findUserById (id: Guid) : Task<Result<User, AppError>> = ...
let validateUser (user: User) : Result<ValidatedUser, AppError> = ...
let saveUser (user: ValidatedUser) : Task<Result<unit, AppError>> = ...

let processUser id = taskResult {
    let! user   = findUserById id          // Task<Result<_, _>>
    let! valid  = validateUser user        // Result<_, _> (elevated automatically? No! See below)
    do! saveUser valid                     // Task<Result<_, _>>
    return user
}
```

Wait — there's a subtlety. The `Bind` method only accepts `Task<Result<_, _>>`. You cannot `let!` a pure `Result<_, _>` directly inside `taskResult { }`. If you try, you'll get a type mismatch.

How does the Winslow code handle this? They wrap the sync `Result` into a `Task`:

```fsharp
member _.Bind(m: Result<'a, 'e>, f: 'a -> Task<Result<'b, 'e>>) =
    task {
        match m with
        | Ok v    -> return! f v
        | Error e -> return Error e
    }
```

This overload allows mixing sync `Result` and async `Task<Result>` in the same block.

---

## 5. Standard Computation Expressions

### seq { }

The `seq` CE builds `IEnumerable<T>` lazily. It's built into F# and requires no custom builder.

```fsharp
let numbers = seq {
    1
    2
    3
}
// seq [1; 2; 3]

let fibonacci = seq {
    yield 0
    yield 1
    let mutable a, b = 0, 1
    while true do
        let c = a + b
        yield c
        a <- b
        b <- c
}
// Lazy infinite sequence: 0, 1, 1, 2, 3, 5, 8, ...

let multiplied = seq {
    for x in 1..3 do
        for y in 1..3 do
            yield x * y
}
// 1, 2, 3, 2, 4, 6, 3, 6, 9
```

Key difference: `seq` is **lazy** — elements are computed on demand. Use `yield` to produce individual items, `yield!` to yield an entire subsequence.

```fsharp
let flatten xss = seq {
    for xs in xss do
        yield! xs  // yield! flattens
}
```

### async { }

The `async` CE models asynchronous computations as `Async<'T>` — a **workflow** that has not started yet. This is the legacy F# async model (pre-dating .NET Task).

```fsharp
open System.IO

let readFileAsync path = async {
    use stream = File.OpenRead path
    use reader = new StreamReader(stream)
    let! content = reader.ReadToEndAsync() |> Async.AwaitTask
    return content
}

// Running:
let content = readFileAsync "file.txt" |> Async.RunSynchronously
```

Key methods:

| Method | Purpose |
|--------|---------|
| `Async.Start` | Fire-and-forget on thread pool |
| `Async.RunSynchronously` | Block current thread until done |
| `Async.StartAsTask` | Convert to `Task<T>` |
| `Async.AwaitTask` | Convert a `Task` to `Async` |
| `Async.Parallel` | Run multiple async in parallel |

`async { }` uses its own scheduler and can capture `SynchronizationContext`. It does **not** integrate naturally with .NET `Task`-based libraries without explicit conversion (`Async.AwaitTask`, `Async.StartAsTask`).

### task { }

The `task { }` CE models asynchronous computations as `Task<'T>`. It was introduced in F# 6 and is now the preferred way to write async code in F#, especially when interoperating with .NET libraries, ASP.NET, or any modern C# code.

```fsharp
open System.Threading.Tasks

let fetchData url = task {
    use client = new HttpClient()
    let! response = client.GetStringAsync url
    return response.Length
}
```

We cover `task { }` in depth in the next section.

---

## 6. Deep Dive: task { }

### 6.1 How task { } Works

`task { }` is backed by `FSharp.Core.TaskBuilder` (built into FSharp.Core since F# 6). It compiles to efficient state machine code, similar to how C# `async`/`await` works.

```fsharp
open System.Threading.Tasks

let example = task {
    let! x = Task.FromResult 42
    let! y = Task.Run(fun () -> 100)
    return x + y
}
```

This desugars (conceptually) to something like:

```fsharp
let example =
    TaskBuilder.TaskBuilder().Bind(
        Task.FromResult 42,
        fun x ->
            TaskBuilder.TaskBuilder().Bind(
                Task.Run(fun () -> 100),
                fun y ->
                    TaskBuilder.TaskBuilder().Return(x + y)
            )
    )
```

In practice, the compiler generates an efficient **state machine** (like C# does) rather than nesting lambdas, to avoid allocation overhead.

### 6.2 task vs async

| Aspect | `async { }` | `task { }` |
|--------|-------------|------------|
| Type | `Async<'T>` | `Task<'T>` (or `Task`) |
| Interop with C# | Needs `Async.StartAsTask` / `Async.AwaitTask` | Direct |
| Cancellation | `CancellationToken` via `Async` parameter | Via `CancellationToken` in context |
| SynchronizationContext | Captured by default | Not captured (like C# `await`) |
| Performance | More allocations | More efficient |
| Status | Legacy (still supported) | Preferred for new code |

#### When to use which

- **Use `task { }`** when: writing ASP.NET handlers, calling C# libraries, working with `HttpClient`, `Stream`, or any modern .NET API.
- **Use `async { }`** when: working with legacy F# libraries, need `Async.StartChild` or `Async.Parallel` combinators, or need `SynchronizationContext` capture (e.g., UI threads).

### 6.3 Cancellation and SynchronizationContext

`task { }` respects `CancellationToken` via the ambient `CancellationToken` available in ASP.NET contexts. You can access it via:

```fsharp
open System.Threading

let fetchWithCancellation url = task {
    let ct = System.Threading.CancellationToken()
    use client = new HttpClient()
    let! response = client.GetStringAsync(url, ct)
    return response
}
```

In ASP.NET (Falco, Giraffe, etc.), the `HttpContext.RequestAborted` token is typically available. However, `task { }` does **not** automatically capture `SynchronizationContext` (unlike `async { }`), which means:

- No need for `.ConfigureAwait(false)` — it's the default.
- Safe to use in library code without risk of deadlocks.
- Requires manual marshalling back to UI thread in desktop apps.

### 6.4 Real-World Patterns

#### Pattern 1: Sequential Composition

```fsharp
let processOrder orderId = task {
    let! order   = db.FetchOrder orderId
    let! user    = db.FetchUser order.UserId
    let! invoice = billing.GenerateInvoice order user
    do! email.SendInvoice user.Email invoice
    return invoice
}
```

Each step awaits the previous one. This is the most common pattern.

#### Pattern 2: Parallel Composition

```fsharp
let loadDashboard userId = task {
    let userTask   = db.FetchUser userId
    let ordersTask = db.FetchOrders userId
    let statsTask  = analytics.GetStats userId

    let! user   = userTask
    let! orders = ordersTask
    let! stats  = statsTask

    return { User = user; Orders = orders; Stats = stats }
}
```

Function calls inside `task { }` run eagerly — `db.FetchUser userId` starts the task immediately (returns a hot `Task`). `let!` only awaits it, it does not start it. By calling all three functions before any `let!`, all three tasks are already in-flight, achieving concurrency.

#### Pattern 3: try/with inside task { }

```fsharp
let safeFetch url = task {
    try
        use client = new HttpClient()
        let! response = client.GetStringAsync url
        return Ok response
    with
    | :? HttpRequestException as ex ->
        return Error ex.Message
}
```

#### Pattern 4: using resource management

```fsharp
let readFirstLine path = task {
    use stream = File.OpenRead path
    use reader = new StreamReader(stream)
    let! line = reader.ReadLineAsync()
    return line
}
```

The `use` keyword calls `.Dispose()` when the task completes or is cancelled (analogous to C# `using`).

#### Pattern 5: Cancellation

```fsharp
let fetchWithTimeout url timeoutMs = task {
    use cts = new CancellationTokenSource(timeoutMs)
    try
        use client = new HttpClient()
        let! response = client.GetStringAsync(url, cts.Token)
        return Ok response
    with :? OperationCanceledException ->
        return Error "Request timed out"
}
```

---

## 7. Rising Complexity Examples

### Level 1: Simple Wrapping

```fsharp
// Wrap a value in a computation expression
let example1 = result { return 42 }
// val example1: Result<int, 'a> = Ok 42

let example2 = task { return "hello" }
// val example2: Task<string>
```

### Level 2: Sequencing

```fsharp
// Sequence operations with let!
let example3 = result {
    let! x = Ok 10
    let! y = Ok 20
    return x + y
}
// Ok 30
```

### Level 3: Error Handling with Result

```fsharp
type ValidationError =
    | EmptyField of string
    | OutOfRange of string * int * int

let notEmpty fieldName (s: string) = result {
    if String.IsNullOrWhiteSpace s then
        return! Error (EmptyField fieldName)
    else
        return s
}

let inRange fieldName min max (value: int) = result {
    if value < min || value > max then
        return! Error (OutOfRange (fieldName, min, max))
    else
        return value
}

let validatePerson name age = result {
    let! validName = notEmpty "name" name
    let! validAge  = inRange "age" 0 120 age
    return sprintf "%s is %d years old" validName validAge
}

// Ok "Alice is 30 years old"
// Error (EmptyField "name")
// Error (OutOfRange ("age", 0, 120))
```

Error short-circuiting in action: if name validation fails, age validation never runs.

### Level 4: Async + Result Combined

This is the pattern used in Winslow. Domain functions return `Result`, repository calls return `Task<Result>`.

```fsharp
type UserId = UserId of Guid
type AppError =
    | NotFound of string
    | ValidationError of string * string

// Sync validation (returns Result)
let validateEmail email = result {
    if email.Contains "@" then return email
    else return! Error (ValidationError ("email", "invalid format"))
}

// Async repository (returns Task<Result>)
let fetchUser (id: UserId) : Task<Result<User, AppError>> = ...
let saveUser (user: User) : Task<Result<unit, AppError>> = ...

// Combined handler
let updateEmail userId newEmail = taskResult {
    let! user    = fetchUser userId
    let! email   = validateEmail newEmail
    let updated  = { user with Email = email }
    do! saveUser updated
    return updated
}
```

Flow:
```
fetchUser userId       -> Task<Result<User, AppError>>
  |-- if Error -> short-circuit, return Error
  |-- if Ok    -> unwrap User, continue

validateEmail newEmail -> Result<string, AppError>
  |-- if Error -> short-circuit, return Error wrapped in Task
  |-- if Ok    -> unwrap string, continue

saveUser updated       -> Task<Result<unit, AppError>>
  |-- if Error -> short-circuit, return Error
  |-- if Ok    -> return updated as Result
```

### Level 5: Nested Computation Expressions

```fsharp
let complexWorkflow = taskResult {
    let! user = fetchUser (UserId userId) |> TaskResult.mapError (fun _ ->
        AppError.NotFound "User not found")

    // We can nest another result CE for validation
    let! validation = result {
        let! name  = validateName user.Name
        let! email = validateEmail user.Email
        return {| Name = name; Email = email |}
    }

    let! existing = checkDuplicateEmail validation.Email

    if existing then
        return! Error (AppError.Conflict "Email already exists")
    else
        let! saved = persistUser { user with Name = validation.Name; Email = validation.Email }
        do! publishEvent (UserUpdated saved.Id)
        return saved
}
```

This demonstrates mixing `taskResult { }` for async operations with nested `result { }` for sync validation, alongside control flow (`if/then/else`) and early `return! Error` for guard clauses.

---

## 8. Builder Method Reference

```fsharp
type CompleteBuilder() =
    // Core
    member _.Bind(m, f)         = ...   // let! x = m
    member _.Return(x)          = ...   // return x
    member _.ReturnFrom(m)      = ...   // return! m
    member _.Zero()             = ...   // else branch with no explicit return

    // Sequencing
    member _.Combine(a, b)      = ...   // a; b
    member _.Delay(f)           = ...   // lazy wrapping

    // Loops
    member _.For(xs, f)         = ...   // for x in xs
    member _.While(cond, body)  = ...   // while cond

    // Exceptions
    member _.TryWith(body, handler)    = ... // try ... with
    member _.TryFinally(body, comp)    = ... // try ... finally
    member _.Using(res, body)          = ... // use x = res
```

### Minimal Builder

You only need `Bind` and `Return`:

```fsharp
type IdentityBuilder() =
    member _.Bind(m, f) = f m
    member _.Return(x)  = x

let identity = IdentityBuilder()

let demo = identity {
    let! x = 10
    let! y = 20
    return x + y
}
// val demo: int = 30
```

### Builder with Side Effects (Logging)

```fsharp
type LoggingResultBuilder(log: string -> unit) =
    member _.Bind(m: Result<'a, 'e>, f: 'a -> Result<'b, 'e>) =
        match m with
        | Ok v ->
            log $"Binding Ok: %A{v}"
            f v
        | Error e ->
            log $"Binding Error: %A{e}"
            Error e
    member _.Return(x) =
        log $"Returning: %A{x}"
        Ok x
    member _.ReturnFrom(m: Result<'a, 'e>) =
        log $"ReturnFrom: %A{m}"
        m
    member _.Zero() = Ok ()

let loggedResult = LoggingResultBuilder(printfn "%s")

let example = loggedResult {
    let! a = Ok 1
    let! b = Ok 2
    return a + b
}
// Output:
// Binding Ok: 1
// Binding Ok: 2
// Returning: 3
```
