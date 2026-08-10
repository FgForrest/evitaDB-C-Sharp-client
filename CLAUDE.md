# Catching up the main database flow

This app repository: https://github.com/FgForrest/evitaDB-C-Sharp-client
EvitaDB repository: https://github.com/FgForrest/evitaDB

This repository is a C# client for EvitaDB.
It had not been updated almost for two years.

This repository should follow public API / gRPC implementation from a role of a gRPC client - as a driver for the database. 
The implementation should follow C# conventions, so it does not need to be 1:1 implementation per-se, but it should be as close as possible while respecting C# conventions.
Functionality should be the same, that should be convered by tests, similar one that is implemented in EvitaDB repository for the driver, only here with usage of xUnit while following already standardized way.

It operates via gRPC, uses convertors, gRPC types, protobuf types, and EvitaDB types.