---
title: Modern enterprise search
description: The main kinds of search used in enterprise systems, how each works, and where each belongs.
document:
  theme: curiosity
  running: Modern enterprise search
  author: Curiosity
  company: Curiosity GmbH
---

--- {layout="cover" art="field" running="Whitepaper" foot="curiosity.ai/resources"}

# Modern enterprise search *Search is not one thing.*

The main kinds of search used in enterprise systems, how each works, and where each belongs. Written for the architects who have to choose.

::: facts
Topic
:   Search architecture

For
:   Architects and engineering leads

Date
:   October 2026
:::

--- {layout="contents" running="Contents"}

## Six layers, and where each one belongs

:::: entries
::: entry {num="01" title="Search is not one thing" page="03"}
Why most disagreements are about which layer is meant.
:::

::: entry {num="02" title="Where keyword still wins" page="06"}
Exact retrieval for part numbers and identifiers.
:::

::: entry {num="03" title="What vectors add" page="09"}
Meaning beyond the words a field report uses.
:::

::: entry {num="04" title="Hybrid and graph retrieval" page="11"}
Ranking that knows how records relate.
:::

::: entry {num="05" title="Agents that search" page="13"}
Search as a tool, under the user’s permissions.
:::

::: entry {num="06" title="How Curiosity implements it" page="15"}
All six layers, in one system.
:::

::: entry {num="·" title="Glossary" page="20" muted="true"}
:::
::::

::: note {label="In one paragraph" ground="stone"}
Most arguments about search are about which layer is meant. A part number needs an exact match. A field report needs meaning. An agent needs both, and the permissions of the person asking. This paper takes the layers apart so you can choose them on purpose.
:::

--- {layout="opener" art="squares" number="01"}

# Search is<br>not one thing

- Why most arguments about search are about which layer is meant
- The six layers, from exact match to agents
- What each layer needs from the data underneath

--- {layout="statement" running="01 · Search is not one thing"}

1.1 {.doc-label}

### Six layers, one question each

::: claim
Name the layer before you argue about search. Most disagreements end there.
:::

::::: cols {count="3"}
:::: col
Ask five engineers what search means and you get five systems. One means the box that finds a part number. One means a ranked list. One means a chat window that answers in sentences.

The layers build on each other. An agent is only as good as the search it calls. A hybrid ranking is only as good as the two lists it merges.
::::

:::: col
Keyword search matches the words in the question to the words in a record. Fuzzy search allows for typos and variants. Vector search matches meaning, so a description finds a record written in other words.

So the first design decision is not which product to buy. It is which layers your questions need, and what each one needs from the data underneath.
::::

:::: col
Hybrid search ranks both kinds of match in one list. Graph retrieval follows the links between records: a part, the notice about it, the tickets it caused. Agents use all of these as tools.

::: note {label="Layer"}
One way of deciding which records match a question, and in what order.
:::
::::
:::::

::: flow {caption="Fig. 1.1 · The six layers of search, each building on the last."}
- 01 · Keyword | Keyword | Exact terms, BM25 || 02 · Fuzzy | Fuzzy | Typos, variants || 03 · Vector | Vector | Meaning, not wording || 04 · Hybrid | Hybrid | Both, one ranking || 05 · Graph | Graph | How records relate || [solid] 06 · Agents | Agents | Search as a tool
:::

--- {layout="table" running="01 · Search is not one thing"}

Table 1.1 {.doc-label}

## Which layer finds what

| Layer | Finds | Misses | Explains itself | Handles wording |
| --- | --- | --- | --- | --- |
| Keyword | Exact ids, part numbers | Synonyms, wording | [!mark yes] | [!mark part] |
| Fuzzy | Typos, variants | Meaning | [!mark yes] | [!mark part] |
| Vector | Meaning, descriptions | Exact ids | [!mark no] | [!mark yes] |
| Hybrid | Both, in one list | Relations between records | [!mark part] | [!mark yes] |
| Graph | How records relate | Free text alone | [!mark yes] | [!mark yes] |
| Agents | Multi-step questions | Nothing, if built on the rest | [!mark part] | [!mark yes] |

[!mark yes] Yes [!mark part] In part [!mark no] No {.doc-legend}

::: note {label="Read it this way" ground="stone"}
No row is all solid. Each layer covers a gap the one above it leaves, which is the case for running them together rather than choosing one.
:::

1.2 {.doc-label}

### What each layer needs from the data

:::: cols
::: col
Keyword and fuzzy search need clean text fields and the identifiers kept intact. Vector search needs the meaning in the text, so scanned PDFs and field reports have to be read first.

The graph needs the links between records: which part a notice is about, which ticket it caused. Agents need all of it, plus the permissions of the person asking.
:::

::: col
::: note {label="Before you choose"}
List the questions people ask today. Mark each with the layer it needs. The layers you mark most are the ones to build first.
:::
:::
::::

--- {layout="horizon" art="field" number="02" dx=".3" dy=".5"}

# Where keyword<br>still wins

- Why exact retrieval is still the right tool for part numbers
- How BM25 scores a match, term by term
- One query against two indexes

--- {layout="plate" running="02 · Where keyword still wins"}

::::: cols {class="doc-head-cols"}
:::: col
2.1 {.doc-label}

## How BM25 scores a match
::::

:::: col
A technician types `A320-2741-08`. There is one right answer, and a near miss is a wrong part on the line. Keyword retrieval finds the exact string, fast, and can say why it matched.
::::
:::::

::: flow {plate="stone" caption="Fig. 2.1 · The six layers of search. This section is about the first."}
- [mark] 01 · Keyword | Keyword | Exact terms, BM25 || 02 · Fuzzy | Fuzzy | Typos, variants || 03 · Vector | Vector | Meaning, not wording || 04 · Hybrid | Hybrid | Both, one ranking || 05 · Graph | Graph | How records relate || [solid] 06 · Agents | Agents | Search as a tool
:::

::::: cols
:::: col
Each query term adds to the score. A rare term adds more than a common one. A term adds less each time it repeats, and a long document is damped so it cannot win by size alone.

Vector search is built to find what is close. For an identifier, close is the failure. That is why a production system keeps a keyword index beside the vector one.

Keyword retrieval also explains itself. A match is a set of terms that appear in the record, so an engineer can see why a result ranked where it did.

Its cost is vocabulary. A field report that says hydraulic leak will not match a ticket that says fluid loss. Section 03 picks up there.
::::

:::: col
::: keypoint
Keep keyword for identifiers. Add vectors for meaning. Never replace one with the other.
:::

::: note {label="BM25"}
Ranks by how often a term appears, damped by document length and by how common the term is.
:::
::::
:::::

--- {layout="technical" running="02 · Where keyword still wins"}

2.2 {.doc-label}

## One query, two indexes, one ranking

The same request runs against both indexes. The exact match is boosted, the close matches follow, and the user's permissions are applied before anything is ranked.

::: figure {caption="Listing 2.1 · Illustrative, not the API"}
```csharp
// one query, two indexes, one ranking
var q = Query.Parse("A320-2741-08 fluid loss");
var hits = graph.Search(q)
    .Keyword(boost: 2.0)   // exact ids
    .Vector(k: 50)         // meaning
    .AsUser(ctx.User);     // permissions
```
:::

| Query | Keyword | Vector | Right tool |
| --- | --- | --- | --- |
| `A320-2741-08` | Exact | Near misses | Keyword |
| `fluid loss, aft` | Misses synonyms | Finds leaks | Vector |
| `leak on 2741-08` | Partial | Partial | Hybrid |

2.3 {.doc-label}

### When the exact match is missing

:::: cols
::: col
A query for a part number that is not in the index should return nothing, and say so. A list of near misses invites the wrong part.

A good system shows the empty result and offers the close matches as a separate list, marked as close, so the engineer chooses with open eyes.
:::

::: col
::: note {label="Design rule"}
Exact results and close results are two lists until a person or a ranking rule decides they are one.
:::
:::
::::

--- {layout="opener" art="bars" number="03" dx=".64"}

# What vectors<br>add

- How a record becomes a point in a space of meaning
- Why close is right for field reports and wrong for part numbers
- What it costs to run, and to explain

--- {layout="sidebar" running="03 · What vectors add"}

3.1 {.doc-label}

## Meaning instead of words

A field report says the aft door seal was weeping fluid. The ticket about the same fault says hydraulic leak, door 4. No word is shared, so keyword search treats them as unrelated.

Vector search turns each record into a point in a space where distance stands for meaning. Records about the same fault land close together, whatever words their authors chose.

It also costs more to explain. A keyword match lists the terms it found. A vector match is a distance, and a reviewer has to trust it or open the record.

::::: cols {ratio="1:1.1" class="doc-stack-cols"}
:::: col
::: flow {layout="compact"}
- 01 · Keyword | Keyword | Exact terms, BM25
- 02 · Fuzzy | Fuzzy | Typos, variants
- [mark] 03 · Vector | Vector | Meaning, not wording
- 04 · Hybrid | Hybrid | Both, one ranking
- 05 · Graph | Graph | How records relate
- [solid] 06 · Agents | Agents | Search as a tool
:::
::::

:::: col
That is right for descriptions and wrong for identifiers. Part 2741-08 and part 2741-09 sit close in meaning, and they are different parts.

Use it where people describe, beside the keyword index, and let a hybrid ranking set the order. Section 04 shows how.

Fig. 3.1 · The six layers. This section is about the third. {.doc-caption}
::::
:::::

::::: aside
:::: note {label="HNSW"}
An index that finds the nearest vectors quickly, without comparing the question against every record.
::::

:::: keypoint
Use vectors where people describe a thing. Keep keyword where they name it.
::::
:::::

--- {layout="horizon" art="window" number="04"}

# Hybrid and<br>graph retrieval

- Exact and close matches, ranked as one list
- Following a part to its notice and its tickets
- The query path, step by step

--- {layout="diagram" running="04 · Hybrid and graph retrieval"}

::::: cols {ratio="1:1"}
:::: col
Figure 4.1 {.doc-label}

## The query path
::::

:::: col
One question, from the moment it is typed to the answer with its sources. Keyword and vector run side by side.
::::
:::::

::: flow {layout="path" label="The query path"}
- 01 · Question | Which A320 parts failed after the March notice? | Typed by an engineer in technical support
- 02 · Permissions | Only what this engineer may see | Access synced from each source system
- 03 · Keyword | Exact: A320, the notice id | BM25 over every text field || 03 · Vector | Close: failed, fault, removed | Meaning beyond the wording
- [mark] 04 · Hybrid rank | Both lists, one ranking | Exact matches first, close ones after
- 05 · Graph | Part, to notice, to ticket | Records joined by how they relate
- [solid] 06 · Answer | 14 parts, each with its source record | Every line traces to a ticket or a log
:::

::::: cols {count="3" style="notes"}
::: note {label="02"}
Permissions apply before search, so a result the engineer may not open is never ranked.
:::

::: note {label="04"}
The marked step: exact and close matches ranked as one list, exact first.
:::

::: note {label="06"}
The answer cites records, so a reviewer can open each source.
:::
:::::

--- {layout="opener" art="field" number="05"}

# Agents that<br>search

- Search as a tool an agent calls, several times
- Why permissions have to hold at every step
- What a reviewer needs to see afterward

--- {layout="plate" running="05 · Agents that search"}

::::: cols {class="doc-head-cols"}
:::: col
5.1 {.doc-label}

## An agent calls search, again and again
::::

:::: col
Ask an agent which suppliers are behind the delays on the A320 line. It does not search once. It finds the delayed orders, then the parts on them, then the supplier notices about those parts.
::::
:::::

::: flow {plate="stone" caption="Fig. 5.1 · The six layers. This section is about the last."}
- 01 · Keyword | Keyword | Exact terms, BM25 || 02 · Fuzzy | Fuzzy | Typos, variants || 03 · Vector | Vector | Meaning, not wording || 04 · Hybrid | Hybrid | Both, one ranking || 05 · Graph | Graph | How records relate || [mark] [solid] 06 · Agents | Agents | Search as a tool
:::

::::: cols
:::: col
Each of those steps is a search, and each one runs as the person who asked. If one step ignores permissions, the answer can hold a record the user could never open.

That is why permissions belong in the search layer, not in the agent. An agent that filters afterward has already read what it should not have seen.

The agent also needs exact matches. A supplier notice is found by its id, and a close match on an id is the wrong notice.

Finally, every step leaves a trail. A reviewer should be able to open each search the agent ran, with its results, and see how the answer was built.
::::

:::: col
::: keypoint
An agent is only as careful as the search it calls. Permissions hold at every step, or not at all.
:::

::: note {label="Tool call"}
A request the agent makes to another system, here a search, with the user’s identity attached.
:::
::::
:::::

--- {layout="horizon" art="squares" number="06" dx=".8" dy=".5"}

# How Curiosity<br>implements it

- All six layers in one system, on your infrastructure
- Where it runs today, and for whom
- How to start, in three steps

--- {layout="quote" running="Customer story"}

::: quote {by="Services Innovation Team · Airline Services at Airbus"}
![Airbus](assets/airbus.svg)

“By intelligently enhancing our search efficiency, Curiosity lets Airbus technical support quickly find information across millions of documents.”
:::

:::: cols {style="foot"}
::: note {label="The problem" plain="true"}
Forty years of technical data across CRM systems, network drives and engineering databases, in a vocabulary of acronyms and reference numbers that defeated ordinary search.
:::

::: note {label="What changed" plain="true"}
One entry point over every source, the engineering references extracted and joined in a graph, filters by aircraft type, program and reference number.
:::
::::

--- {layout="numbers" running="In production"}

## Running where the data is.

Across Curiosity's production deployments, on premises and in private clouds.

:::: stats
::: stat {value="30TB+" label="Data connected in production" ground="paper"}
:::

::: stat {value="20,000+" label="Active users across deployments" ground="deep"}
:::

::: stat {value="15%+" label="Efficiency gain in production workflows" ground="slate"}
:::

::: stat {value="70+" label="Enterprise systems connected" ground="stone"}
:::
::::

--- {layout="steps" running="06 · How Curiosity implements it"}

## Three steps, in this order

:::: steps
::: step {num="01" ground="ink"}
Index every identifier field for exact match first.
:::

::: step {num="02" ground="deep"}
Add vectors where people describe instead of name.
:::

::: step {num="03" ground="stone"}
Rank both together, then follow the graph.
:::
::::

Before you choose {.doc-label}

- [ ] Which fields hold identifiers: part numbers, notice ids, serials?
- [ ] Where do people write in their own words: field reports, tickets?
- [ ] Who may see which record, and in which source system is that set?
- [ ] Who needs to check why a result was returned?

--- {layout="takeaways" running="In short"}

## What to take from this paper

:::: entries {style="takeaways"}
::: entry {num="01" title="Name the layer first."}
Most arguments about search are about which layer is meant.
:::

::: entry {num="02" title="Exact and close, ranked together."}
Keyword for identifiers, vectors for descriptions, one list.
:::

::: entry {num="03" title="Permissions in the search, not after it."}
Every step an agent takes runs as the person who asked.
:::
::::

--- {layout="glossary" running="Glossary"}

## Terms used in this paper

:::: glossary
::: term {name="BM25"}
A keyword ranking: frequent terms in short records score higher, common terms count less.
:::

::: term {name="Fuzzy match"}
A match that allows small differences in spelling, such as a typo in a part name.
:::

::: term {name="HNSW"}
An index that finds the nearest vectors quickly without comparing against every one.
:::

::: term {name="Hybrid ranking"}
One list ranked from keyword and vector results together.
:::

::: term {name="Knowledge graph"}
Records and the typed links between them: a part, the notice about it, the ticket it caused.
:::

::: term {name="Permission-aware"}
Results are filtered by what the user may see in the source system, at query time.
:::

::: term {name="Vector search"}
Search by meaning: records close in meaning to the question, whatever their words.
:::

::: term {name="Agent"}
A program that uses search as a tool to answer a question in several steps.
:::
::::

--- {layout="back" running="Curiosity" head-right="Built in Munich · Runs in your data center" foot="hello@curiosity.ai|Curiosity GmbH · Baaderstr. 19 · 80469 Munich"}

# Describe it Monday.<br>Ship it this week.

Curiosity connects tickets, part records, maintenance logs and field reports into one permission-aware graph, and serves it to the model you choose, on premises or in your private cloud.

[!tag text="curiosity.ai/request-demo" tone="solid"]

::: facts
Data residency
:   EU, on premises or private cloud

Compliance
:   GDPR

Member
:   KI Bundesverband
:::
