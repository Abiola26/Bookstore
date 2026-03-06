---
description: Fix MSB3027/MSB3021 errors (file lock) by killing ghost processes
---
This workflow terminates any running Bookstore API or .NET processes that might be locking build artifacts.

1. Kill locking processes
// turbo
```powershell
Get-Process Bookstore.API, dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
```

2. Clean and Build
// turbo
```powershell
dotnet clean; dotnet build
```
