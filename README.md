# NerderyDotNetChallenges

A repository to house the Nerdery's .NET challenges.

Challenges are housed under the **challenges** folder, and executing them is simple:

```PowerShell
fsi Challenges/Challenge_[DATE].fsx
```

## Other stuff

This code is also running in the cloud at https://nerdery-dotnet-challenges.herokuapp.com.  Each \*.fsx file has been built into an HTML page by compilation.  Running this site locally requires the following:

```Powershell
./paket.exe install
fsi build.fsx
fsi app.fsx
```

What this does is:
 - Installs dependencies from Nuget using [Paket](http://fsprojects.github.io/Paket/), an optimized dependency manager.
 - Builds HTML files from \*.fsx files using [FSharp.Formatting](http://tpetricek.github.io/FSharp.Formatting/), a library that champions [Literate Programming](http://en.wikipedia.org/wiki/Literate_programming), and does it *well*.  It also builds markdown files.
 - The **app.fsx** file runs a [Suave.IO](http://suave.io/) server, which very succinctly serves HTML/CSS files to the user.  This supports [Razor](https://github.com/SuaveIO/suave/blob/f5a0c5cdd9c1b29353e778b3e6bb3c049f9ba96b/src/Suave.Razor/Library.fs) syntax, although undocumented.  I'm just using it for file serving.

## Deployment considerations

Since this code is running on Heroku, setup and deployment is *ridiculously* simple.  Essentially, from square one:

1. Install the [Heroku toolbelt](https://toolbelt.heroku.com/)

2. Run the following commands from within this directory
```Powershell
heroku login
heroku create nerdery-dotnet-challenges --buildpack https://github.com/SuaveIO/mono-script-buildpack.git 
heroku config:set PORT=80
git push heroku master
```

And that's all folks!
