module internal FsFaker.Helpers

open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

let changeUnderlyingCollectionType<'c, 'p when 'p: not struct and 'c :> IEnumerable<'p>> (items: 'p seq) =

    let t = typedefof<'c>

    let col: IEnumerable<'p> =
        if t = typedefof<list<_>> then
            List.ofSeq items
        elif t = typedefof<array<'p>> then
            Array.ofSeq items
        elif t = typedefof<ResizeArray<_>> then
            items.ToList()
        elif t = typedefof<LinkedList<_>> then
            LinkedList items
        elif t = typedefof<ImmutableArray<_>> then
            items.ToImmutableArray()
        elif t = typedefof<ImmutableList<_>> then
            items.ToImmutableList()
        elif t = typedefof<HashSet<_>> || t = typedefof<ISet<_>> then
            items.ToHashSet()
        elif t = typedefof<IReadOnlyCollection<_>> || t = typedefof<IReadOnlyList<_>> then
            List.ofSeq items
        elif t = typedefof<IList<_>> || t = typedefof<ICollection<_>> then
            items.ToList()
        else
            List.ofSeq items

    box col :?> 'c
