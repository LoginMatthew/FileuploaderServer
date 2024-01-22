# Fileuploader Server

A simple application which is enable at home to store file by the server and client.
People are able to store files and check the uploaded files and able to download them which are uploaded.
The layout of the pages is also optimized for mobile use. This program is perfect to use to share files between family members at home.

## The server project

Data is stored on a given place by using MySQL DB (Code First approach).
The Backend project is API .NET Core 6.0.
Password is encrypted with using salt.
Data objects' transfer are encrypted.
Only the appropriate and authorized users are able to access given data.

## Project packages and used software:
<ul>
  <li>Microsoft.AspNetCore.Authentication.JwtBearer(Version:6.0.4)</li>
  <li>Microsoft.EntityFrameworkCore (Version:7.0.11)</li>
  <li>Microsoft.EntityFrameworkCore.Tools (Version:7.0.11)</li>
  <li>MySql.EntityFrameworkCore (Version:7.0.5)</li>
  <li>Istalled and used MySQL server Ver. 8.0.31</li>
</ul>

## Miggration (Package Manager Console)

0. Enter your MYSQL Connnection information in the "appsettings.json"'s "DefaultConnection" part.

(Create Migration)
1. EntityFrameworkCore\Add-Migration InitialCreate -Context DataFileDbContext
2. EntityFrameworkCore\Add-Migration InitialCreate -Context UserDbContext

(Export to MYSQL DB)
3. EntityFrameworkCore\Update-Database -context DataFileDbContext
4. EntityFrameworkCore\Update-Database -context UserDbContext

## Development Server

Modify IP addresses in the project files to access by angular client project

*** Additional descripion about the project is on the 'about' page in the related client project. ***
