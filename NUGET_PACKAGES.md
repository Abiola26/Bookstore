# NuGet Packages Required

## Bookstore.Infrastructure.csproj

Add the following to the project file:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.3" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.0.3" />
    <PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" Version="7.0.3" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Http" Version="2.2.2" />
</ItemGroup>
```

## Bookstore.API.csproj

Add the following to the project file:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.0" />
</ItemGroup>
```

## Installation Command

Run in Package Manager Console:

```powershell
# Update Bookstore.Infrastructure
Install-Package Microsoft.EntityFrameworkCore -Version 10.0.0
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 10.0.0
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 10.0.0
Install-Package System.IdentityModel.Tokens.Jwt -Version 7.0.3
Install-Package Microsoft.IdentityModel.Tokens -Version 7.0.3
Install-Package BCrypt.Net-Next -Version 4.0.3

# Update Bookstore.API
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 10.0.0
Install-Package Swashbuckle.AspNetCore -Version 7.0.0
```

Or update using dotnet CLI:

```bash
# From Bookstore.Infrastructure folder
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.3
dotnet add package Microsoft.IdentityModel.Tokens --version 7.0.3
dotnet add package BCrypt.Net-Next --version 4.0.3

# From Bookstore.API folder
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
dotnet add package Swashbuckle.AspNetCore --version 7.0.0
```
