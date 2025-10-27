# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# .NET 8.0 license management system that validates program usage time limits using internet time (NTP) to prevent local time manipulation. The system encrypts license data using AES encryption and stores it in a fixed location (`C:\Jarch25\license.dat`).

## Build and Run Commands

### Build the project
```bash
dotnet build
```

### Run the program
```bash
dotnet run
```

### Clean build artifacts
```bash
dotnet clean
```

## Architecture

The codebase consists of three main components:

### Core Components

1. **LicenseHelper.cs** (`ProgramLicenseManager.LicenseHelper`)
   - Core license management functionality
   - NTP time validation using multiple servers (time.google.com, time.windows.com, pool.ntp.org, time.nist.gov)
   - AES encryption/decryption for license storage
   - License file path: `license.dat` (in current working directory)
   - Key methods: `CreateLicense()`, `CheckLicense()`, `GetLicenseInfo()`

2. **Program.cs** (`ProgramLicenseManager.Program`)
   - Interactive console application for testing the license system
   - Menu-driven interface for license creation, validation, and information display
   - Example of how to integrate license checking into a program

3. **IntegrationExample.cs** (`ProgramLicenseManager.IntegrationExample`)
   - Demonstrates how to integrate the license system into real applications
   - Shows both comprehensive and simple integration patterns
   - Example error handling and user messaging

### Key Architecture Patterns

- **Namespace**: All code uses `ProgramLicenseManager` namespace
- **Static Helper Pattern**: `LicenseHelper` class provides static methods for all license operations
- **Defensive Security**: Uses NTP servers for time validation to prevent local time manipulation
- **Encryption**: AES-256 encryption with SHA-256 key derivation from a configurable secret key
- **Fallback Strategy**: Multiple NTP servers with automatic failover

## Important Configuration

### Security Configuration
- **Encryption Key**: Located in `LicenseHelper.cs` line 19 - `ENCRYPTION_KEY` constant should be changed for production use
- **License File Path**: `license.dat` in current working directory (line 17) - uses `Directory.GetCurrentDirectory()`

### NTP Servers
The system uses multiple NTP servers for time validation (lines 27-32 in LicenseHelper.cs):
- time.google.com
- time.windows.com
- pool.ntp.org
- time.nist.gov

## Development Notes

- **Target Framework**: .NET 8.0 with nullable reference types enabled
- **Dependencies**: Uses only .NET standard libraries (no external NuGet packages)
- **Output Directory**: Custom build output path configured to `bin` folder
- **Console Application**: Entry point in `Program.cs` with interactive menu system

## Testing the System

The main program provides three test modes:
1. License creation with expiration date input
2. License validation and program execution simulation
3. License information display

Use `dotnet run` and select option 1 to create a test license, then option 2 to validate it.