using System;
using BCrypt.Net;

string password = "Password123!";
string hash = BCrypt.HashPassword(password);
Console.WriteLine($"Password: {password}");
Console.WriteLine($"BCrypt Hash: {hash}");

// Verify it works
bool isValid = BCrypt.Verify(password, hash);
Console.WriteLine($"Verification: {isValid}");
