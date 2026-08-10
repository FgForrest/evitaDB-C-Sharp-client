# Catching up the main database flow

On [issues](https://github.com/FgForrest/evitaDB-C-Sharp-client/issues) page, there is a list of issues to catch up with newer evitaDB versions up to some point. 
We have some little parts of that implemented, you may or may not use that. 

I want you to analyze evitaDB repository, you have it available, cloned and updated in its dev branch in ~/www/evita/evitaDB.
Want all driver-related stuff to be ported to this repository - in a similar way it is implemented up to this point.
Implement missing public API, datatypes, tests, and functionality from 2024.4 up to 2026.2.4.

All methods / calls on a network layer should also have their async counterparts, if possible without unnecessary code duplication.

I want you to also update this project to Dotnet 10.