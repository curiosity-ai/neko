---
title: The similarity engine, end to end
description: The similarity engine, from raw text to a ranked list — the graph it reads, the NLP that turns sentences into edges, the embeddings that turn fields and topology into vectors, and the fusion that weighs them against each other.
presentation:
  eyebrow: Curiosity Workspace
  back: /presentations/presentations
---

# Similar to what, exactly?

The similarity engine, from raw text to a ranked list — the graph it reads, the NLP that turns sentences into edges, the embeddings that turn fields and topology into vectors, and the fusion that weighs them against each other.

> A similarity score here is not one number from one index. It is a sum of named signals, and every result carries the breakdown that produced it.

--- {eyebrow="The problem"}

## One vector index answers one question

"Find me something like this" is never a single question. Ask it about a support case and you could mean any of these — and a single cosine distance can only mean one of them.

Reads like this one
:   Similar words, similar symptoms, similar phrasing. Text embeddings answer this.

Touches the same things
:   Same device, same part number, same ATA chapter. That lives in the graph — once something has put it there.

Behaves like this one
:   Same shape of connections rather than same words. Graph topology answers this.

Matters to this user
:   Their fleet, their region, their open work. A personal signal, not a corpus-wide one.

And not like that
:   Cases already closed as duplicates, products the customer returned. Something has to push results down as well as up.

--- {eyebrow="The whole path"}

## From a committed node to a ranked, explained list

::: figure {label="Pipeline: sources feed the graph; the graph feeds an NLP pipeline, a sentence embedding index and a PageSpace graph embedding index; all three feed signals, which are fused, filtered by rules, and produce a scored result."}
```embed
<svg viewBox="0 0 880 300">
  <defs>
    <marker id="a2" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
      <path d="M0 0 L8 4 L0 8 z" fill="#2F5478"/>
    </marker>
  </defs>
  <g font-family="IBM Plex Mono, monospace" font-size="11" fill="#93AAC6">
    <rect x="4" y="128" width="80" height="44" rx="4" fill="none" stroke="#2F5478"/>
    <text x="44" y="147" text-anchor="middle">connectors</text>
    <text x="44" y="162" text-anchor="middle">+ files</text>
    <line x1="86" y1="150" x2="104" y2="150" stroke="#2F5478" marker-end="url(#a2)"/>

    <rect x="108" y="118" width="96" height="64" rx="4" fill="none" stroke="#5FD0D8"/>
    <text x="156" y="142" text-anchor="middle" fill="#5FD0D8">graph</text>
    <text x="156" y="158" text-anchor="middle">nodes + edges</text>
    <line x1="206" y1="150" x2="226" y2="66" stroke="#2F5478" marker-end="url(#a2)"/>
    <line x1="206" y1="150" x2="226" y2="150" stroke="#2F5478" marker-end="url(#a2)"/>
    <line x1="206" y1="150" x2="226" y2="234" stroke="#2F5478" marker-end="url(#a2)"/>

    <rect x="230" y="40" width="196" height="52" rx="4" fill="none" stroke="#2F5478"/>
    <text x="328" y="60" text-anchor="middle" fill="#DCE6F2">NLP pipeline</text>
    <text x="328" y="78" text-anchor="middle">spotters → _Mentions edges</text>

    <rect x="230" y="124" width="196" height="52" rx="4" fill="none" stroke="#2F5478"/>
    <text x="328" y="144" text-anchor="middle" fill="#DCE6F2">sentence embeddings</text>
    <text x="328" y="162" text-anchor="middle">field text → HNSW</text>

    <rect x="230" y="208" width="196" height="52" rx="4" fill="none" stroke="#2F5478"/>
    <text x="328" y="228" text-anchor="middle" fill="#DCE6F2">PageSpace embeddings</text>
    <text x="328" y="246" text-anchor="middle">topology → HNSW</text>

    <line x1="428" y1="66" x2="452" y2="140" stroke="#2F5478" marker-end="url(#a2)"/>
    <line x1="428" y1="150" x2="452" y2="150" stroke="#2F5478" marker-end="url(#a2)"/>
    <line x1="428" y1="234" x2="452" y2="162" stroke="#2F5478" marker-end="url(#a2)"/>

    <rect x="456" y="118" width="96" height="64" rx="4" fill="none" stroke="#5FD0D8"/>
    <text x="504" y="142" text-anchor="middle" fill="#5FD0D8">signals</text>
    <text x="504" y="158" text-anchor="middle">+ / −</text>
    <line x1="554" y1="150" x2="574" y2="150" stroke="#2F5478" marker-end="url(#a2)"/>

    <rect x="578" y="118" width="80" height="64" rx="4" fill="none" stroke="#2F5478"/>
    <text x="618" y="147" text-anchor="middle" fill="#DCE6F2">fuse</text>
    <text x="618" y="163" text-anchor="middle">weights</text>
    <line x1="660" y1="150" x2="678" y2="150" stroke="#2F5478" marker-end="url(#a2)"/>

    <rect x="682" y="118" width="72" height="64" rx="4" fill="none" stroke="#2F5478"/>
    <text x="718" y="147" text-anchor="middle" fill="#DCE6F2">rules</text>
    <text x="718" y="163" text-anchor="middle">filter only</text>
    <line x1="756" y1="150" x2="774" y2="150" stroke="#2F5478" marker-end="url(#a2)"/>

    <rect x="778" y="106" width="98" height="88" rx="4" fill="none" stroke="#E9A94A"/>
    <text x="827" y="132" text-anchor="middle" fill="#E9A94A">ScoreInfo</text>
    <text x="827" y="150" text-anchor="middle">score</text>
    <text x="827" y="166" text-anchor="middle">+ components</text>
    <text x="827" y="182" text-anchor="middle">per signal</text>
  </g>
</svg>
```
:::

::: note
Everything left of `signals` happens at ingest time and is shared by the whole workspace. Everything right of it happens per request, in a scenario you write.
:::

--- {eyebrow="Stage 1 · The graph"}

## Edges are the part that carries meaning

Two of the three similarity sources read the graph rather than the text. What ends up in it — and how densely — decides what the engine can say.

Typed nodes
:   Every node has a type and a stable key, addressed by a `UID128`. Endpoint code reaches them through generated constants: `N.SupportCase.Type`, `N.Device.Type`.

Typed edges, both ways
:   Relationships are first-class and stored as pairs, so a traversal is cheap in either direction: `E.ManufacturedBy` / `E.Manufactures`, `E.Placed`, `E.Contains`.

Traversal as a query
:   `Q().StartAt(uid).Out(N.Tag.Type, E.HasTag).Out(N.Product.Type, E.HasTag)` — a chain that is also, later, a similarity signal.

Density matters
:   Structural similarity is only as good as the edges. A node with two relationships is nearly indistinguishable from any other node with two relationships.

--- {eyebrow="Stage 2 · NLP"}

## A pipeline per language, bound to fields

A pipeline is the NLP stack the workspace runs over a field's text — tokenizer, POS tagger, spotter models, pattern spotters, entity linker, post-processing. It runs at commit, automatically.

:::: cols
::: box {title="How it is wired up"}
- Built under **Settings → NLP → Pipelines**, one per language, in **Data Parsing**, **Conversational** or **Custom** mode.
- Assigned to node-field pairs on the **Used for** tab: `SupportCase → Content`, `KbArticle → Body`.
- Every value of those fields flows through it — new ingest during the commit, history via reparse.
:::

::: box {title="Mixed languages"}
- Assign the same field to several pipelines and each ingest is routed by the language detector.
- For a field with more than one language inside it, the workspace splits at sentence level and routes each sentence on its own.
- Changing a spotter, a model or a linking rule means a reparse — it walks every document. Pause other ingestion while it runs.
:::
::::

--- {eyebrow="Stage 2 · Entity linking"}

## Where a sentence becomes an edge

> Spotters find phrases. Linking is what turns a found phrase into a relationship the graph can traverse.

Configured per type
:   Under **Management → Data → [type] → Linking**, enabled for specific pipeline sources. Spotters carry no linking of their own.

The edges it writes
:   `_AppearsIn` and `_Mentions` by default, overridable to match your domain model. After that a mention is just a traversal: `.Out("_Mentions")`.

Auto-creation
:   Pattern-based entities — IDs, part numbers, reference codes — can create the node when it is missing, so the graph fills itself in as the text is read.

Longest match wins
:   Matches apply longest-first, so `ATA-53-40` links as itself rather than as the `ATA-53` inside it.

Also in chat replies
:   The same configuration can run over assistant replies via `ChatViewConfiguration.ParseAs`, and every linked node is access-checked per reader — a mention of a record they cannot see stays plain text.

--- {eyebrow="Stage 3 · Sentence embeddings"}

## Field text in, HNSW vectors out

`SentenceEmbeddingsIndex` reads one text field off each node, encodes it, and stores the vector in an HNSW index. Use it when the content of the field is what makes two nodes alike.

| Encoder | Runs | Context |
| --- | --- | --- |
| `MiniLM` | in-process, CPU or GPU | ~256 tokens. Fast, low-RAM. |
| `ArcticXS` | in-process, CPU or GPU | ~512 tokens. The default for new indexes; higher recall at comparable cost. |
| `External` | remote HTTP | Any OpenAI-compatible endpoint, up to 4096 tokens. The text leaves the workspace. |

::: note
The encoder is fixed once the index exists — switching models means a new index. Long values can be chunked into overlapping windows, each chunk stored with its parent UID, and chunk hits dedupe back to the parent at query time. `PrefixFieldName` puts a title in front of *every* chunk so short queries still match.
:::

--- {eyebrow="Stage 3 · Graph embeddings"}

## PageSpace: vectors from shape, not words

A StarSpace-style model trained on random walks through the graph. Nodes that co-occur on a walk are pulled together, randomly sampled nodes pushed apart, and each node ends up with one vector.

:::: cols
::: box {title="You define what \"similar\" means"}
- `EdgesToFollow` and `NodesToFollow` set the walk topology. Those two lists *are* the definition of similarity for your domain.
- `Dimensions`, `Epoch`, `LearningRate`, `NegativeSamplingCount` tune the training itself.
- Answers questions text cannot: which customers behave alike, which products are functionally substitutable.
:::

::: box {title="Two things to plan for" tone="warn"}
- **It is not trained on commit.** Call `TrainAsync` once there is meaningful data, then again on a schedule as the graph shifts. New nodes get a vector predicted in real time without retraining.
- **It is sensitive to density.** Sparse subgraphs produce poor vectors. Every node wants at least a handful of real relationships — which is exactly what stage 2 supplies.
:::
::::

--- {eyebrow="Stage 3 · Choosing"}

## Which vector answers which question

| The question | The right tool |
| --- | --- |
| Find products with similar names or descriptions | Sentence embeddings |
| Find customers who behave like this one — same purchase patterns, same cases | Graph embeddings |
| Find substitute products based on who buys them | Graph embeddings |
| You already have vectors from a domain-specific model | Raw embeddings |
| Find explicit paths or connected components | Plain `IQuery` traversal — not embeddings at all |
| Any mix of the above, weighed against each other | The similarity engine |

::: note
All of them implement the same index surface, so a consumer never has to care which kind of vector it is querying.
:::

--- {eyebrow="Stage 4 · Signals"}

## A signal is a source of candidates with scores

The engine lives in `Mosaik.GraphDB.Similarity` and is reached through `IQuery.ToSimilarity(...)`. A scenario starts at a seed, declares signals, fuses them, and filters the result.

| Source | How |
| --- | --- |
| Text embeddings | `StartAtSimilarTextAsync(text, count, nodeTypes, indexUID)` — returns a scored query |
| Graph traversal | A standard chain from `ctx.Subjects` — the seed the scenario started at |
| External lookup | Call out to anything async, return an `IQuery` over the matching UIDs |
| Pre-scored hits | Return `IEnumerable<ScoredUID>` and feed your own numbers in |

::: note
Scores live on the query, so they survive narrowing — `Where`, `Except`, `IsRelatedTo`, `Take`. Traversals carry them too: a score propagates to every UID it reaches, and a node reached from several sources merges them as √(x² + y²) / 1.4. That is what lets one signal pivot from similar products to their manufacturers and still rank them meaningfully.
:::

--- {eyebrow="Stage 4 · Fusion"}

## Three places scores combine

:::: cols
::: box
### Within one signal

A signal may declare several sources. By default it sums them. `UsingReciprocalRankFusion` switches to `1 / (k + rank)` instead, which is the right call when the sources' raw scores are not on the same scale.

### Across the positive signals

One fuse function, set with `Fuse(...)`. A signal's `Weight(...)` is applied before fusion, so it scales that signal's contribution under any fuse.
:::

::: box
| Fuse | Effect |
| --- | --- |
| `Sum` | Add the signal scores. The default. |
| `Max` | Keep the strongest single signal — one source of truth, others as tiebreakers. |
| `Min` | Keep the weakest. |
| `Euclidean` | √(Σaᵢ²) — a soft OR. |
| `Product` | Multiply — a soft AND; a candidate must score on every signal. |
:::
::::

--- {eyebrow="Stage 4 · Demoting and filtering"}

## Pushing down is not the same as filtering out

Negative signals are scored exactly like positive ones but fused into their own group, then combined with the positive group by `FuseFinal(...)`. They only adjust candidates a positive signal already surfaced — they never introduce new ones.

| Final fuse | Effect |
| --- | --- |
| `Subtract` | positive − negative. The default. |
| `SubtractScaled(w)` | positive − w · negative — tune the penalty strength. |
| `Discount` | positive / (1 + negative) — scale down, never amplify. |
| `Decay(rate)` | Exponential falloff as the negative grows. |
| `DropIfNegativeOver(t)` | Zero the candidate past a threshold. |

::: note {tone="limit"}
Rules are the other tool, and they only filter — they never change a score. For a scoring adjustment reach for a signal or a custom fuse, not a rule.
:::

--- {eyebrow="Stage 5 · Explainability"}

## Every score carries its own receipt

The engine tracks attribution through an automatic-differentiation pass, so `ScoreInfo.Components` holds each signal's contribution by name, alongside the final score.

The components add up
:   For the built-in fuses the per-signal components sum exactly to the score, and a negative signal shows up as a negative entry — so the breakdown explains what pulled a result down as well as up.

Except when they can't
:   Homogeneous-degree-one operations (Sum, Max, Min, Euclidean, the means) keep that property. `Product`, `Pow` and `Sigmoid` still attribute, but the components read as each signal's *sensitivity* rather than an additive share.

It reaches the UI
:   `SearchArea.WithSimilarityEngine(...)` renders each hit with a ContributionBar built from those components — labelled with the names you gave in `AddSignal`. Name signals for the reader, not for yourself.

And the clock
:   `TrackTimings(true)` fills `result.Timings` per signal, per rule and for fusion. `TrackProgress` streams stage events you can relay to the caller mid-request.

--- {eyebrow="Stage 5 · The sharp edge" accent="rose"}

## Signals run as admin

> The engine does not enforce per-user access on its results. That is left to the consumer — which means it is left to you.

:::: cols
::: box {title="Why" tone="stop"}
`ctx.Graph` inside a signal is the admin-level graph, because a signal often needs to traverse structure the user cannot read in order to rank things they can. So `result.Scores` may contain UIDs the caller is not allowed to see.
:::

::: box {title="Two ways to close it"}
- **Scope inside the signal** — `ctx.Graph.Query(userUID)` instead of `ctx.Graph.Query()`.
- **Filter the result** — `.FilterAsUser(CurrentUser)` on the scenario drops anything the user cannot access before `ExecuteAsync` returns.
:::
::::

::: note
Unlike ordinary search, permission-aware retrieval here is opt-in. A scenario that ships without one of these two lines is a leak with a ranking attached.
:::

--- {eyebrow="Building one"}

## The order to do it in

:::: cols
::: box
- **1 · Model the edges first.** Structural signals and PageSpace both read them. Text similarity alone is the thing you already had.
- **2 · Point NLP at the fields that carry entities** and configure linking for each type you want traversable. Reparse after every rule change.
- **3 · Index the field that decides likeness**, chunked if it is long, with a prefix field if titles matter. Not every field needs a vector.
:::

::: box
- **4 · Train PageSpace once the graph is dense enough**, and schedule the retrain. Check the walk topology before blaming the vectors.
- **5 · Start with one signal**, read the breakdown, then add the second. Weights tuned against a breakdown you can see beat weights guessed in advance.
- **6 · Add `FilterAsUser` before anyone else sees it.**
:::
::::

::: note
Sources: docs.curiosity.ai — similarity engine, sentence embeddings, graph embeddings (PageSpace), NLP pipelines, entity linking, IQuery similarity search.
:::
