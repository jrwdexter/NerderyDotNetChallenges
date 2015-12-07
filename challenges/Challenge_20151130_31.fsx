(**
# .NET Challenge for November 30, 2015

## Prompt

## Challenge Accepted

The official format for submissions is [https://dotnetfiddle.net](https://dotnetfiddle.net).  Please post a link to your submission in a comment to this post, or via email if (and when) comments still don't work.

This week’s challenge is the fourth in a series of cross-backend technology challenges (JVM, .NET, Ruby, and PHP). Code can be submitted in any language, and each submission will only be graded against like-disciplined submissions.

--------------------

Last year, {INSERT_HOLIDAY_MYTH_HERE} (hereafter referred to as "Santa") created a special [GMO](https://en.wikipedia.org/wiki/Gmo): the **Candy Cane Plant**. However, after creating the plant Santa and his biologist friends found out that it suffered from an extreme deficiency: each plant could only breed once. Each candy cane plant has the following **properties** and **methods**:

```
public class CandyCanePlant
{
  int CandyCanesProducedPerWeek { get; set; }
  bool HasBred { get; set; }
}

public class BreederService
{
  public CandyCanePlant Breed(CandyCanePlant father, CandyCanePlant mother);
}
```

A newly bred candy cane plant has a production value of:

`Math.Pow(fathersProductionValue, mothersProductionValue) % (fathersProductionValue + mothersProductionValue)`

and has a `HasBred` value of **false**.

Write code that optimizes the total number of candy canes produced per week given an initial stock of `CandyCanePlant[]` plants, producing a new collection of `CandyCanePlant[]`s. The output of your code should also produce the total number of candy canes produced per week with the bred stock.

For example, given the following initial stock:

```
CandyCanePlant { CandyCanesPerWeek = 5}
CandyCanePlant { CandyCanesPerWeek = 3}
CandyCanePlant { CandyCanesPerWeek = 2}
```

Your code should inform Santa what his final stock will be, as well as the total production value of said stock. The above example should produce a total production output of

```
CandyCanePlant { CandyCanesPerWeek = 5, HasBred = true}
CandyCanePlant { CandyCanesPerWeek = 5, HasBred = true}
CandyCanePlant { CandyCanesPerWeek = 4, HasBred = false}
CandyCanePlant { CandyCanesPerWeek = 3, HasBred = true}
CandyCanePlant { CandyCanesPerWeek = 2, HasBred = true}
```

and a production value of **19 per week**.

Code will be judged based on a large input stock of Candy Cane Plants.

**Bonus Problem** (hard!):

The above code models an ideal world - in the real world, Candy Cane Plants experience a production reduction of **1 candy cane per week**, until they hit 0 candy canes produced. Assuming that breeding takes **1 week** and produces no candy canes, inform Santa of how many total candy canes will be produced in the optimal breeding scenario.

Note that breeding in certain weeks may produce more optimal plants than other weeks, and that the optimal breeding scenario may not be identical to the solution found above.
----------------------------------------------
*)

(**
## My result
*)

(**
### Types
Here's our candy cane plant type
*)
type CandyCanePlant =
  {
    Productivity : double
    HasBred : bool
  }

(**
  ### Helper methods
  These methods exist to be utilized in the algorithm below, and help make it more idiomatic.
*)
// Pattern match a list that has at least a pair of unbred plants with a tuple of (Unbred,AlreadyBred) plant lists.
let (|HasUnbred|_|) plants =
  match plants |> List.filter (fun p -> p.HasBred = false) with
  | [] -> None
  | [_] -> None
  | x -> Some(x, plants |> List.filter (fun p -> p.HasBred))

// Splice a list into a tuple of (itemAtIndex, restOfList)
let splice index (list:'T list) =
  (
    list.[index],
    list.[0..index-1] @ list.[index+1..list.Length-1]
  )

// Breed two plants together, making a new list of plants (bredMother, bredFather, newChild)
let breed mother father =
  [
    { mother with HasBred = true }
    { father with HasBred = true }
    {
      Productivity = (mother.Productivity ** father.Productivity) % (mother.Productivity + father.Productivity)
      HasBred = false
    }
  ]

// Simple helper methods to make plants from number values
let makePlants list =
  list |> List.map (double) |> List.map (fun i -> {Productivity = i; HasBred = false})

(**
### The algorithm
The big implementation. This is a recursive method that takes in a list of plants, and returns a list of lists of plants (possible outcomes of breedings)
So if we entered plants with production values of [1,2], we should get [[1,2,2],[1,2,1]], since there are two ways to breed this pair.
This method is then `O(n!^2)`, due to combining all permutations of plants, and knowing that order matters.
*)
// Our recursive method to find all breeding plants
let rec findAllBreedingOptions plants =
  seq {
    match plants with
    | HasUnbred (unbredPlants,bredPlants)  ->
      // If we have unbred plants, then
      // yield! is similar to a C# yield, but it returns an Enumerable instead of a single item
      yield! seq {
        for i = 0 to unbredPlants.Length - 1 do
          for j = i + 1 to unbredPlants.Length - 1 do
            // For each pair of unbred plants, splice them out of all unbred plants
            let (unbredTwo,tempUnbred) = unbredPlants |> splice j
            let (unbredOne,unbred) = tempUnbred |> splice i
            // Then, breed them in both orders (using one as a mother, then the other)
            yield! (bredPlants @ unbred @ breed unbredOne unbredTwo) |> findAllBreedingOptions
            yield! (bredPlants @ unbred @ breed unbredTwo unbredOne) |> findAllBreedingOptions
      }
    | _ ->
      // if there aren't any unbred plants, we're done. Let's just return the resutls
      yield plants
  }

(**
### Execution
On my machine, 6 plants take `.5 seconds` to execute (`n*518,400` calculations)

Let's examine memory consumption for a purely immutable model:

Since each record takes double (`8 bytes`) and bool (`1 byte`) of space, that means that a CandyCanePlant is `9 bytes`.
(Weirdly enough, F# optimizes this record to be 8 bytes, not 9. I am confused)
A list in F# is a linked list, meaning it will add another `2 bytes` (I'm using 64 bit code here) to each node.

Each result is `11 plants`, and we have a total of `86,400` result options produced.

#### Memory footprint

  - N plant list memory size = `(N*2-1) * 8 bytes / plant + (N*2-1) * 2 bytes / plant link = 10*(N*2-1)` bytes per result.
  - Number of results = `N * (N-1)!^2`
  - Total memory footprint: `N * (N-1)!^2 * (N*2-1) * 10 = 10 * (N*2 - 1) * N!^2/2`

This is:

  - `.25 Megabytes` for 5 plants
  - `9.06 Megabytes` for 6 plants
  - `450 Megabytes` for 7 plants
  - `227.5 Terabytes` for 10 plants

*)
#time
[1..6] |> makePlants |> findAllBreedingOptions
|> Seq.sortBy (fun l -> l |> List.sumBy (fun c -> c.Productivity))
|> Seq.rev
|> Seq.head

(**
  Timing result: `Real: 00:00:00.627, CPU: 00:00:00.609, GC gen0: 27, gen1: 8, gen2: 1`
*)
