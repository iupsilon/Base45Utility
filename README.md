# Base45Utility

[![CI](https://github.com/iupsilon/Base45Utility/actions/workflows/dotnet.yml/badge.svg)](https://github.com/iupsilon/Base45Utility/actions/workflows/dotnet.yml)

A small, dependency-free Base45 encoding and decoding utility for .NET, implementing
[draft-faltstrom-base45-03](https://datatracker.ietf.org/doc/html/draft-faltstrom-base45-03)
(the encoding used, among others, by the EU Digital COVID Certificate).

## Features

- Encode/decode between raw bytes and Base45 strings.
- String helpers that treat text as UTF-8.
- Strict decoding: rejects illegal characters, invalid lengths, and out-of-range groups.
- Allocation-friendly `Span` overloads on modern targets.
- Multi-targets `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0`.

## Installation

The package is published to **GitHub Packages**. Add the source once (replacing
`USERNAME` and `TOKEN` with a GitHub username and a personal access token that has the
`read:packages` scope):

```bash
dotnet nuget add source "https://nuget.pkg.github.com/iupsilon/index.json" \
  --name github-iupsilon --username USERNAME --password TOKEN --store-password-in-clear-text
```

Then reference it from your project:

```bash
dotnet add package Base45Utility
```

```xml
<PackageReference Include="Base45Utility" Version="1.6.0" />
```

## Usage

### Encoding

`Base45` is a lightweight, stateless type; a single instance can be reused.

```csharp
using Base45Utility;

var base45 = new Base45();

// From a string (encoded as UTF-8)
string encoded = base45.Encode("Hello world");        // "%69 VD82EK4F.KEA2"

// From raw bytes
byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Hello world");
string encodedFromBytes = base45.Encode(bytes);       // same result
```

### Decoding

```csharp
var base45 = new Base45();
const string input = "%69 VD82EK4F.KEA2";

byte[] decodedBytes = base45.Decode(input);           // raw bytes
string decodedText  = base45.DecodeAsString(input);   // "Hello world" (UTF-8)
```

### Span overloads (net8.0 and later)

For hot paths, zero/low-allocation overloads are available:

```csharp
var base45 = new Base45();

// Encode directly from a span of bytes
ReadOnlySpan<byte> payload = stackalloc byte[] { 1, 2, 3 };
string encoded = base45.Encode(payload);

// Decode into a caller-provided buffer; returns the number of bytes written
Span<byte> destination = stackalloc byte[32];
int written = base45.Decode("%69 VD82EK4F.KEA2".AsSpan(), destination);
```

## Behavior and error handling

- String overloads assume **UTF-8** for both directions.
- Empty input round-trips to empty output.
- `null` input throws `ArgumentNullException`.
- `Decode` throws `InvalidOperationException` for an illegal character, an invalid
  length (a Base45 string length is always `3n` or `3n+2`), or a group that decodes
  to a value outside the valid byte/two-byte range.
- The `Span` decode overload throws `ArgumentException` if the destination buffer is
  too small.

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
