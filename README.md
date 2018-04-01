# Portfolio-Samples
Some code examples from my coding projects

## GroundWarLogic.cs and TimberWordsLogic.cs
Functional asynchrounous game programming sample based on my Rx based game engine.  I've published more than 10 mobile games based on this engine which makes it possible to write complex game logic in a simple and maintainable fashion.

![Timber Words](/Images/timberwords.png)
![Aliens vs Bubbles](/Images/aliensvsbubbles.jpg)

My Other Mobile Games
https://omegadot.wordpress.com/


## EntityQueryLanguage
EQL (Entity Query Language) is a fluent API that I've developed to write SQL queries in a strongly typed fashion
against an object model and execute it against various different databases.  Unlike other ORMs,
EQL supports much of the T-SQL and PL-SQL languages.  Many ORM frameworks aim to completely 
isolate database access by abstracting a common subset of the query languages, while EQL starts with
the database and provides a bridge to OO world with a much richer query language.

## SimpleObjectCollaborationFramework
A library that simplifies complex interactions between objects by providing a new mechanism of instance discovery and lifetime management.  The collaboration context will just be accessible along the code execution path until using() block is exited.  All objects that are in the calling method as well as all nested methods will be able to access this shared collaboration context.

