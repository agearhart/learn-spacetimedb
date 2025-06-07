# learn-spacetimedb

A sample project demonstrating a client-server architecture using SpaceTimeDB.

## Projects

### Server

Located in `server/`, this project provides the Spacetime DB instance.

- **Features:**
    - Handles client requests
    - Manages database state
    - Provides real-time updates

#### Getting Started

```bash
dotnet workload install wasi-experimental
cd server
spacetime publish --project-path . tanks
```

Test the server is working by calling a function and checking the logs to see if the user name has been set in the database:
```bash
spacetime call tanks SetUserName "Bloop"
spacetime logs tanks
```

### Client

Located in `client/`, this project is the frontend application that interacts with the server.

- **Features:**
    - Connects to the server API
    - Displays real-time data
    - User-friendly interface

#### Getting Started

```bash
dotnet run --project client
```

## Requirements

- DotNet 8

## Usage

1. Start the server.
2. Start the client.

## License

MIT