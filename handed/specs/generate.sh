#!/bin/sh
echo "Cleaning"
# Clean build directory
rm -r build
echo "Building"
# Make a new one
mkdir build
# Markdown --> LaTeX
pandoc specs.md --template ../template.tex -s -o main.tex
# Move the main.tex to the build area. No idea why pandoc can't put it there
# directly.
mv main.tex build/main.tex
# Also include additional files (images...)
cp include/* build/
echo "Done."
