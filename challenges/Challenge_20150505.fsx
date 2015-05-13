(**
# .NET Challenge for May 5, 2015

## Prompt
We’ll keep things simple for the first .NET code challenge.
Calculate 100 Fibonacci Numbers.

Bonus points for using memoization to optimize the calculation.
The best entry (as arbitrarily decided by me rolling dice) will get accolades in next week’s .NET newsletter.
*)

(**
## Coding
*)

(** 
### Step One: The Fibonacci method
`x` and `y` are the two numbers to perform the Fibonacci addition on.
Unfold creates an infinite series, which essentially can be modeled as the following:
Assuming we have a fold like `Seq.unfold mySuperFunction`,
the THIRD element of that sequence would be
`mySuperFunction(mySuperFunction(mySuperFunction(input)))`.
*)
let sequence = Seq.unfold (fun (x, y) -> 
  Some(x + y, (y, x + y))) // Return the tuple (nextValue, state)

(**
### Step Two: Print our output
We'll take 100 elements, starting from the pair (0,1)
*)
(decimal(0), decimal(1)) |> sequence |> Seq.take 100 |> Seq.iter (fun x -> printfn "%M" x) 

(** 
## Result
```f#
1
2
3
5
...
354224848179261915075
573147844013817084101
```
*)