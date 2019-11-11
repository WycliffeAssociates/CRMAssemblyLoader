# CRMAssemblyLoader
An assembly CD tool for Dynamics CRM

# Desciption
This tool updates a plugin inside of a dynamics environment

# Building
The easiest way to build is to use Visual Studio to compile the solution.

# Usage
This is a command line tool so you are going to need to open an instance of command prompt in order to run it.

Usage:

`CRMAssemblyLoader.exe <connection string> <assembly>`

Example:

`CRMAssemblyLoader.exe "AuthType=Office365;Username=user@domain.com; Password=password; Url=https://environment.crm.dynamics.com/;" Plugin.dll`

This example will load the Plugin.dll assembly into the environment environment.crm.dynamics.com

# Contributing
If you find a bug or need additional functionality please submit an issue. If you are feeling
generous or adventurous feel free to fix bugs or add functionlity in your own fork and send us
a pull request.