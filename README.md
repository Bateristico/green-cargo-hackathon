# Train Yard Manager

A cross-platform mobile application built with .NET MAUI for managing train yard operations, featuring real-time synchronization with Couchbase Mobile.

## Overview

This application helps train yard operators track wagons, manage tasks, and coordinate operations efficiently. It works offline-first with Couchbase Lite and syncs data to the cloud when connected.

## Features

### Wagon Management
- Track wagon details (number, type, location, destination)
- Monitor wagon status (Available, In Transit, Under Inspection, Maintenance)
- Flag wagons requiring legal checks
- Record inspection dates and notes
- Full CRUD operations with offline support

### Task Management
- Create and assign operational tasks
- Track completion status
- Organize work with descriptions and priorities
- Mark tasks as completed

### Data Synchronization
- Local-first architecture using Couchbase Lite
- Bi-directional sync with Couchbase Sync Gateway
- Works offline, syncs when online
- Real-time sync status monitoring

## Technical Stack

- **Framework**: .NET MAUI (Multi-platform App UI)
- **Database**: Couchbase Lite 4.0
- **Sync**: Couchbase Sync Gateway
- **Target Platforms**: Android, iOS, Windows, macOS

## Project Structure

```
CouchbaseHackathonApp/
├── Models/                  # Data models
│   ├── TaskItem.cs
│   ├── Wagon.cs
│   └── TrainYardOperation.cs
├── Views/                   # UI pages
│   ├── TasksListPage.*
│   ├── TaskEditPage.*
│   ├── WagonsListPage.*
│   ├── WagonDetailsPage.*
│   └── SettingsPage.*
├── Services/                # Business logic
│   ├── DatabaseService.cs
│   └── SyncService.cs
├── Platforms/               # Platform-specific code
└── Resources/               # Images, fonts, styles
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022 or later (or VS Code with C# Dev Kit)
- Android SDK (for Android development)
- Xcode (for iOS/macOS development, macOS only)

### Setup

1. Clone the repository:
   ```bash
   git clone https://gitlab.com/kolomolo/genai/hackathons/green-cargo-hackathon.git
   cd green-cargo-hackathon
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Configure Sync Gateway (optional):
   - Open the app and navigate to Settings
   - Enter your Sync Gateway URL
   - Provide authentication credentials

### Running the App

**Android:**
```bash
dotnet build -t:Run -f net10.0-android
```

**iOS:**
```bash
dotnet build -t:Run -f net10.0-ios
```

**Windows:**
```bash
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

## Configuration

### Sync Gateway Setup

The app requires a Couchbase Sync Gateway instance for cloud synchronization. Configure the connection in the Settings tab:

- **URL**: Your Sync Gateway endpoint (e.g., `wss://your-gateway.com/dbname`)
- **Username**: Authentication username
- **Password**: Authentication password

### Test Data

Use the "Generate Test Wagons" button in Settings to create 20 sample wagon entries for testing and demonstration purposes.

## Database Schema

### Wagon Document
```json
{
  "type": "wagon",
  "wagonNumber": "WGN-12345",
  "wagonType": "Freight",
  "status": "Available",
  "currentLocation": "Track A1",
  "destination": "Stockholm",
  "requiresLegalCheck": false,
  "lastInspection": "2025-12-17T10:00:00Z",
  "notes": "Regular maintenance completed",
  "createdAt": "2025-12-17T09:00:00Z"
}
```

### Task Document
```json
{
  "type": "task",
  "title": "Inspect wagon WGN-12345",
  "description": "Perform routine safety inspection",
  "isCompleted": false,
  "createdAt": "2025-12-17T09:00:00Z"
}
```

## Development

### Key Components

**DatabaseService** - Handles all local database operations with Couchbase Lite
- Document CRUD operations
- Query execution
- Collection management

**SyncService** - Manages synchronization with Sync Gateway
- Bi-directional replication
- Connection status monitoring
- Error handling and retry logic

### Adding New Features

1. Define data model in `Models/`
2. Add CRUD methods to `DatabaseService`
3. Create UI pages in `Views/`
4. Wire up navigation in `AppShell.xaml`

## Architecture

The app follows MVVM-like patterns with:
- **Models**: Data structures
- **Services**: Business logic and data access
- **Views**: XAML UI with code-behind
- **Dependency Injection**: Singleton services via MAUI's built-in DI

## Troubleshooting

**Build errors on Android:**
- Ensure Android SDK is properly installed
- Check `Platforms/Android/MainActivity.cs` for proper Couchbase initialization

**Sync not working:**
- Verify Sync Gateway URL is accessible
- Check credentials are correct
- Review sync status in Settings tab

## License

This project is developed for the Green Cargo Hackathon.

## Contributing

This is a hackathon project. For questions or suggestions, please reach out to the development team.
