# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Base45Utility is a small .NET library that implements Base45 encoding/decoding per
[draft-faltstrom-base45-03](https://datatracker.ietf.org/doc/html/draft-faltstrom-base45-03)
(the scheme used by EU Digital COVID Certificates). It is published as a signed NuGet package.

## Commands

All commands run from the `src/` directory (where `Base45.sln` lives).

```bash
# Build the whole solution
dotnet build src/Base45.sln

# Run all tests (across all target frameworks)
dotnet test src/Base45.sln

# Run tests for a single framework only
dotnet test src/Base45Test/Base45Test.csproj -f net8.0

# Run a single test by name
dotnet test src/Base45Test/Base45Test.csproj --filter "FullyQualifiedName~SimpleEncodeBytesTest"

# Produce the NuGet package (also generated automatically on build of the library project)
dotnet pack src/Base45Utility/Base45Utility.csproj -c Release
```

## Architecture

The entire library is one class: `src/Base45Utility/Base45.cs` (`Base45Utility.Base45`).
All tests live in `src/Base45Test/EncodeTest.cs` (NUnit).

Key design points worth knowing before changing the encoding/decoding logic:

- **Chunking:** Encoding processes input 2 bytes at a time → 3 Base45 chars (`EncodedChunkSize`).
  A trailing single byte → 2 chars (`SmallEncodedChunkSize`). Decoding is the inverse:
  3-char chunks → 2 bytes, a trailing 2-char chunk → 1 byte. These chunk-size invariants are the
  core of the algorithm; the `% BaseSize` / `/ BaseSize` arithmetic depends on them.
- **Decode validation:** `FromBase45` is a static 256-entry reverse-lookup table (built once in the
  static constructor, thread-safe) where non-alphabet chars map to `-1`. `Decode` throws
  `InvalidOperationException` on any non-alphabet character or when a 3-char chunk decodes to a value
  `> 0xFFFF` (which cannot represent two bytes).
- **String overloads assume UTF-8:** `Encode(string)` and `DecodeAsString(string)` go through
  `System.Text.Encoding.UTF8`. The Base45 alphabet itself is ASCII, so the encoded string is
  treated as UTF-8 too.
- **Null vs empty:** null inputs throw `ArgumentNullException`; empty inputs round-trip to
  empty results (do not change this — there are tests pinning both behaviors).

When modifying the algorithm, the test suite covers the contract you must preserve: empty input,
single byte (0 and 255), 2/3-byte round-trips, full UTF-8 multibyte strings, all 256 byte values,
a large UTF-8 file fixture (`Base45Test/Utf8TestFile.txt`, copied to output), and invalid-character
rejection. The `"Hello world"` → `"%69 VD82EK4F.KEA2"` vector is the canonical sanity check.

## Packaging / project config

- The library multi-targets `netstandard2.0;net8.0;net9.0;net10.0`; the test project targets
  `net8.0;net9.0;net10.0`. Keep these in sync when bumping framework support.
- The assembly is strong-named (`SignAssembly` + `AssemblyOriginatorKeyFile=SigningKey.snk`).
  `SigningKey.snk` is required to build the library and is committed to the repo.
- Package metadata (version, authors, tags) lives in `Base45Utility.csproj`. Bump
  `AssemblyVersion`, `FileVersion`, and `PackageVersion` together for a release.
</content>
</invoke>
