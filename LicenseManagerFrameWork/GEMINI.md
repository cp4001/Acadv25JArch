# Project Overview

This project is a C++/CLI library for managing software licenses. Its main purpose is to create and validate a license file named `license.dat`.

The license validation process involves the following steps:
1.  The expiration date is encrypted and stored in the `license.dat` file.
2.  The application reads the encrypted expiration date from the file.
3.  To prevent tampering by changing the system clock, the application fetches the current time from a list of public NTP (Network Time Protocol) servers.
4.  The fetched internet time is compared with the decrypted expiration date to check the license validity.

The core logic is implemented in C++/CLI, leveraging the .NET framework for functionalities like cryptography and network communication.

## Key Files

*   `LicenseManager.h`: Defines the public interface for the `LicenseHelper` class, which includes methods for creating, checking, and retrieving license information.
*   `LicenseManager.cpp`: Contains the implementation of the `LicenseHelper` class. This includes the logic for file I/O, encryption/decryption, and NTP time synchronization.
*   `KeyStrings.h`: A header file that stores important constant strings, such as the encryption key and the list of NTP servers.

## Building and Running

**TODO:** Add instructions on how to build and run this project.

There are no solution (`.sln`) or project (`.vcxproj`) files in the current directory. To compile and use this library, you will likely need to create a new C++/CLI project in Visual Studio and include these source files.

## Development Conventions

*   **Namespace:** The code is organized under the `ProgramLicenseManager` namespace.
*   **Static Class:** The main functionality is exposed through a static `LicenseHelper` class, which means you don't need to instantiate it to use its methods.
*   **Separation of Concerns:** Sensitive or configuration-related strings (like the encryption key and NTP server addresses) are kept in a separate `KeyStrings.h` file to be easily managed.
*   **Error Handling:** The methods provide console output for success and failure scenarios, which is useful for debugging.
