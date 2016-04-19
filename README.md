![Pōmōna](https://raw.githubusercontent.com/okb/Pomona/gh-pages/images/pomona-icon-512.png)

# Pomona

Pomona is a framework built for exposing a domain model in a RESTful and hypermedia-driven manner.

It embraces the concept of convention over configuration, and provides opiniated defaults on how
domain model objects is exposed as HTTP resources.

Pomona was born out of frustrations with the difficulties of exposing a complex business domain model
as a RESTful web service.

[![Build status](https://ci.appveyor.com/api/projects/status/vj3cw49n499u6046?svg=true)](https://ci.appveyor.com/project/Pomona/pomona)

## Highlights

* Customizable conventions
* Fluent style overriding for exceptions to rules
* Autogenerated .NET client
* Advanced query filtering
* Simple LINQ provider

## Getting started

Precompiled Pomona packages are available through NuGet.

A new Pomona service can be created by following the steps below:

1. Add a reference to the [Pomona package](https://www.nuget.org/packages/pomona) using NuGet
2. Implement your own `IPomonaDataSource`
3. Inherit from `TypeMappingFilterBase`, and at a minimum implement `GetSourceTypes()` and `GetIdFor()`.
   They're abstract, so you can't miss them.
   `GetSourceTypes()` must return the list of what Types to expose to web service.
4. Inherit `PomonaModule` (which is a Nancy module), and treat this as you normally would treat a Nancy module.
   Which could mean zero configuration. Just because Nancy is *that* awesome!
5. Inherit PomonaConfigurationBase and fill in the abstracts.

Look at the Critter example in the source code for details. If you fire up the `Pomona.Example.ServerApp.exe`, it expose the critters on port 2211.
When ServerApp is running go here with a web browser to see what Pōmōna is all about:

* `http://localhost:2211/critters`
* `http://localhost:2211/critters?$expand=critter.hat`
* `http://localhost:2211/Critters.Client.dll` - Generates a client library on-the-fly
* `http://localhost:2211/Critters.Client.1.0.0.0.nupkg` - Generates a [NuGet Package](http://www.nuget.org/) for the client library on-the-fly
* `http://localhost:2211/schemas` - Returns the JSON schema for the transformed data model

You can also `POST` to `http://localhost:2211/critter` create a new critter entity.

## Methods supported

* GET single resource
* GET and filter a collection of resources
* POST a new resource to a collection
* DELETE an existing resource
* PATCH an existing resource

## Project links

* [Homepage](http://pomona.io)
* [GitHub page](https://github.com/okb/pomona)
* [Bug reporting](https://github.com/okb/pomona/issues)
* [![Join the chat at https://gitter.im/Pomona/Pomona](https://badges.gitter.im/Pomona/Pomona.svg)](https://gitter.im/Pomona/Pomona?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)


## Further reading

Documentation for Pomona is unfortunately still mostly lacking. Until the situation improves the best reference point are the example model and tests found in the source.

Although used for internal API of production systems, Pōmōna should still be considered a work in progress.

## Aknowledgements

Thanks to

* [JSON.NET](ttp://james.newtonking.com/projects/json-net.aspx) for serialization stuff. h
* [Nancy](http://nancyfx.org/) for hosting the web service.
  I really love Nancy! I can't overstate how good I think it is! &lt;3 &lt;3 &lt;3
  One day I hope Pōmōna will offer a Super-Duper-Happy path just like it.
* [NUnit](http://www.nunit.org/) for testing.
* [Cecil](http://www.mono-project.com/Cecil) for IL generation in client

## Contributions

Pomona has mostly been built by @BeeWarloc (Karsten N. Strand) and @asbjornu (Asbjørn Ulsberg).

A significant portion of the development is funded by OKB AS.
