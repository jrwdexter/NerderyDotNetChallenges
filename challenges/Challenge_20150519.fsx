(**
# .NET Challenge for May 19, 2015

## Prompt

The official format for submissions is [https://dotnetfiddle.net](https://dotnetfiddle.net). Please post a link to your submission in a comment on this post.

For this week’s challenge, create a function that will return a list of valid dates according to the rules below. The function should accept the following parameters:

  - **repeatInterval**: The frequency at which the event should repeat. This should be an integer representing the number of weeks before another event should be scheduled. This should be an integer.
  - **daysOfWeek**: An array of the days of the week on which an event should be scheduled. Week days should be represented by an integer between 0 and 6. Day 0 is Sunday. Day 6 is Saturday.
  - **startDate**: The date on which to start creating events. Complete all valid dates remaining in this date’s week after the start date. For example, if the start date were a Wednesday and the days of the week had a Tuesday and Thursday, the function would add Thursday and then jump ahead by the repeatInterval before scheduling Tuesday. If this value were a Thursday and the days of the week had only a Tuesday, the first scheduled date would be after the repeatInterval.
  - **endDate**: The date on which to stop creating events.

Feel free to select different parameter names that are either more idiomatically correct for the platform or more aesthetically pleasing to you.

For an example of the kind of UI this would be intended to support, check out the “Repeat” dialog in the Google’s Calendar application when creating an event.
*)

(*** hide ***)
open System;

(**
### Step One: Let's setup inputs
*)
let startDate = DateTime.Now
let endDate = DateTime.Now.AddDays(60.0)
let repeatInterval = 2.0
let daysOfWeek = [DayOfWeek.Sunday; DayOfWeek.Friday]

(**
### Step Two: The actual methods

Our first helper method simply returns a tuple of the current day and the next day (skipped by interval) if the input `dateTime` is less than the `endDate`. 
If it's greater than the end date, a `None` [option type](https://msdn.microsoft.com/en-us/library/dd233245.aspx) is returned.

The weird order of arguments here is for piping reasons: since F# supports [curring](http://fsharpforfunandprofit.com/posts/currying/), the *last argument* is the one we want to be piping in via a `|>` operator.
*)

let getNextDay repeatInterval (endDate:DateTime) (dateTime:DateTime) =
    match dateTime < endDate with
    | true -> Some(dateTime, dateTime.AddDays(7.0 * repeatInterval))
    | false -> None

(**
Next, we have the actual funciton.  What this does is maps the `daysOfWeek` argument onto the current week, to get days of the current week that we want repeated over the interval.

After this, we `collect` (think `.SelectMany()` in C#) a set of [unfold](http://geekswithblogs.net/MarkPearl/archive/2010/06/23/f-seq.unfold.aspx).

Unfold is exactly like C#'s `.Aggregate()` method, *except* that it returns **each** value it aggregates over - it returns an enumerable.  So by selecting many, we're flattening the set of all intervally-repeated days present.

Finally, we filter (to make sure we don't have any trailing or previous-of-this-week entries) and sort (just because).

Therefore, our process looks like:

  - Map `daysOfWeek` to this week's `DateTime` objects
  - Repeat each of those days over `endDate` - `startDate`
  - Filter for trailing & preceeding days
  - Sort
*)
let getDaysOfWeek repeatInterval daysOfWeek endDate (startDate:DateTime) =
    daysOfWeek |> Seq.map (fun wd -> startDate.AddDays(-1.0 * float startDate.DayOfWeek + float wd))
    |> Seq.collect (Seq.unfold (getNextDay repeatInterval endDate))
    |> Seq.filter (fun dt -> dt > startDate && dt < endDate)
    |> Seq.sort

(** 
### Step Three: Do the thing

Simple call to the method and iteration to display it.  We'll add some fancy display logic to make it a little more legible.
*)
let dateDisplay (dt:DateTime) = sprintf "%s - %s" (dt.DayOfWeek.ToString()) (dt.ToString())
dateDisplay startDate |> printfn "Start date - %s\n-----------------------------"

getDaysOfWeek repeatInterval daysOfWeek endDate startDate |> Seq.iter (fun x -> dateDisplay x |> printfn "%s")

dateDisplay endDate |> printfn "-----------------------------\nEnd date - %s"

(**
## Result
```
Start date - Wednesday - 5/20/2015 6:55:56 PM
-----------------------------
Friday - 5/22/2015 6:55:56 PM
Sunday - 5/31/2015 6:55:56 PM
Friday - 6/5/2015 6:55:56 PM
Sunday - 6/14/2015 6:55:56 PM
Friday - 6/19/2015 6:55:56 PM
Sunday - 6/28/2015 6:55:56 PM
Friday - 7/3/2015 6:55:56 PM
Sunday - 7/12/2015 6:55:56 PM
Friday - 7/17/2015 6:55:56 PM
-----------------------------
End date - Sunday - 7/19/2015 6:55:56 PM
```
*)

(**
Mess with this code further on the [.NET Fiddle](https://dotnetfiddle.net/QCDkY6)
*)
