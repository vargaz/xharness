#!/bin/bash -e

mkdir tools
cd tools

dotnet new tool-manifestanifest

dotnet tool install --version 1.0.0-ci --add-source .. Microsoft.DotNet.XHarness.CLI

dotnet xharness ios test --working-directory="workdir" --output-directory="outdir" --app="/
path/to/app/bundle" --targets=ios-simulator-64
